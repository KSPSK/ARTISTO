export const artworkCategories = [
  ["Painting", "Painting"],
  ["Drawing", "Drawing"],
  ["Photography", "Photography"],
  ["Sculpture", "Sculpture"],
  ["DigitalArt", "Digital art"],
  ["Other", "Other"],
];

export function readListingFilters(searchParams) {
  return {
    q: searchParams.get("q") ?? "",
    category: searchParams.get("category") ?? "",
    maxPrice: searchParams.get("maxPrice") ?? "",
  };
}

export function listingFiltersActive(filters) {
  return Boolean(
    filters.q.trim() || filters.category.trim() || filters.maxPrice.trim()
  );
}

export function listingSearchPath(filters) {
  const params = new URLSearchParams();
  const query = filters.q.trim();
  const category = filters.category.trim();
  const maxPrice = filters.maxPrice.trim();

  if (query) params.set("q", query);
  if (category) params.set("category", category);
  if (maxPrice) params.set("maxPrice", maxPrice);

  const text = params.toString();
  return text ? `/artworklistings?${text}` : "/artworklistings";
}

export function artworkListingsApiUrl(filters) {
  const path = listingSearchPath(filters);
  return path.replace("/artworklistings", "/api/artworklistings");
}

export function returnToListings(state) {
  if (state?.listingsPath) return state.listingsPath;
  if (state?.fromSearch) {
    return `/artworklistings?q=${encodeURIComponent(state.fromSearch)}`;
  }
  return "/artworklistings";
}
