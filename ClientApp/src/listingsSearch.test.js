import {
  artworkListingsApiUrl,
  listingFiltersActive,
  listingSearchPath,
  readListingFilters,
  returnToListings,
} from "./listingsSearch";

const noFilters = { q: "", category: "", maxPrice: "" };

describe("readListingFilters", () => {
  it("reads the search, category and max price from the URL", () => {
    const params = new URLSearchParams(
      "q=sunset&category=Painting&maxPrice=50"
    );

    expect(readListingFilters(params)).toEqual({
      q: "sunset",
      category: "Painting",
      maxPrice: "50",
    });
  });

  it("uses empty values for filters that are not in the URL", () => {
    expect(readListingFilters(new URLSearchParams(""))).toEqual(noFilters);
  });
});

describe("listingFiltersActive", () => {
  it("is false when every filter is empty or only spaces", () => {
    expect(listingFiltersActive(noFilters)).toBe(false);
    expect(
      listingFiltersActive({ q: "   ", category: " ", maxPrice: "  " })
    ).toBe(false);
  });

  it.each([
    ["search", { ...noFilters, q: "sunset" }],
    ["category", { ...noFilters, category: "Painting" }],
    ["max price", { ...noFilters, maxPrice: "50" }],
  ])("is true when only the %s filter is set", (_name, filters) => {
    expect(listingFiltersActive(filters)).toBe(true);
  });
});

describe("listingSearchPath", () => {
  it("returns the plain listings path when no filter is set", () => {
    expect(listingSearchPath(noFilters)).toBe("/artworklistings");
  });

  it("includes only the filters that are set", () => {
    expect(listingSearchPath({ ...noFilters, category: "Sculpture" })).toBe(
      "/artworklistings?category=Sculpture"
    );
  });

  it("trims the values and keeps the filters in a fixed order", () => {
    expect(
      listingSearchPath({
        q: "  sunset ",
        category: "Painting",
        maxPrice: " 50 ",
      })
    ).toBe("/artworklistings?q=sunset&category=Painting&maxPrice=50");
  });

  it("encodes characters that are not safe in a URL", () => {
    const path = listingSearchPath({ ...noFilters, q: "blue & gold" });

    expect(path).toBe("/artworklistings?q=blue+%26+gold");
    expect(readListingFilters(new URLSearchParams(path.split("?")[1])).q).toBe(
      "blue & gold"
    );
  });
});

describe("artworkListingsApiUrl", () => {
  it("points the same filters at the API", () => {
    expect(artworkListingsApiUrl(noFilters)).toBe("/api/artworklistings");
    expect(
      artworkListingsApiUrl({ q: "sunset", category: "", maxPrice: "50" })
    ).toBe("/api/artworklistings?q=sunset&maxPrice=50");
  });
});

describe("returnToListings", () => {
  it("returns to the filtered list the visitor came from", () => {
    expect(
      returnToListings({
        fromSearch: "sunset",
        listingsPath: "/artworklistings?q=sunset&category=Painting",
      })
    ).toBe("/artworklistings?q=sunset&category=Painting");
  });

  it("falls back to the search text when there is no saved path", () => {
    expect(returnToListings({ fromSearch: "blue & gold" })).toBe(
      "/artworklistings?q=blue%20%26%20gold"
    );
  });

  it("returns the plain listings path when there is no state", () => {
    expect(returnToListings(undefined)).toBe("/artworklistings");
    expect(returnToListings({})).toBe("/artworklistings");
  });
});
