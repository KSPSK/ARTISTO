import React from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ArtworkListings } from "./ArtworkListings";
import { jsonResponse } from "../testUtils";

const painting = {
  id: 1,
  title: "Sunset over Vilnius",
  creatorName: "Ona",
  userId: 4,
  price: 40,
  category: "Painting",
  imageUrl: null,
};

const digitalArt = {
  id: 2,
  title: "Neon river",
  creatorName: "Jonas",
  userId: null,
  price: 25,
  category: "DigitalArt",
  imageUrl: null,
};

function renderListings(path = "/artworklistings") {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <ArtworkListings />
    </MemoryRouter>
  );
}

function expectLastRequest(url) {
  expect(global.fetch).toHaveBeenLastCalledWith(url, expect.anything());
}

beforeEach(() => {
  global.fetch = jest.fn(() => jsonResponse([]));
});

describe("ArtworkListings filters", () => {
  it("fills the filter fields from the URL and requests the filtered listings", async () => {
    renderListings("/artworklistings?q=sunset&category=Painting&maxPrice=50");

    expect(screen.getByLabelText("Search")).toHaveValue("sunset");
    expect(screen.getByLabelText("Category")).toHaveValue("Painting");
    expect(screen.getByLabelText("Max price (€)")).toHaveValue(50);

    await waitFor(() =>
      expectLastRequest(
        "/api/artworklistings?q=sunset&category=Painting&maxPrice=50"
      )
    );
  });

  it("requests listings with the chosen filters when the form is submitted", async () => {
    const user = userEvent.setup();
    renderListings();
    await screen.findByText(/No listings yet/);

    await user.type(screen.getByLabelText("Search"), "sunset");
    await user.selectOptions(screen.getByLabelText("Category"), "Digital art");
    await user.type(screen.getByLabelText("Max price (€)"), "50");
    await user.click(screen.getByRole("button", { name: "Search" }));

    await waitFor(() =>
      expectLastRequest(
        "/api/artworklistings?q=sunset&category=DigitalArt&maxPrice=50"
      )
    );
  });

  it("does not request filtered listings until the form is submitted", async () => {
    const user = userEvent.setup();
    renderListings();
    await screen.findByText(/No listings yet/);

    await user.selectOptions(screen.getByLabelText("Category"), "Painting");

    expect(global.fetch).toHaveBeenCalledTimes(1);
    expectLastRequest("/api/artworklistings");
  });

  it("clears every filter and requests all listings", async () => {
    const user = userEvent.setup();
    renderListings("/artworklistings?q=sunset&category=Painting&maxPrice=50");
    await screen.findByText("No listings match these filters.");

    await user.click(screen.getByRole("button", { name: "Clear" }));

    expect(screen.getByLabelText("Search")).toHaveValue("");
    expect(screen.getByLabelText("Category")).toHaveValue("");
    expect(screen.getByLabelText("Max price (€)")).toHaveValue(null);
    await waitFor(() => expectLastRequest("/api/artworklistings"));
  });
});

describe("ArtworkListings results", () => {
  it("shows a loading message while the listings are requested", () => {
    global.fetch = jest.fn(() => new Promise(() => {}));

    renderListings();

    expect(screen.getByText("Loading listings...")).toBeInTheDocument();
  });

  it("shows the listings returned by the API", async () => {
    global.fetch = jest.fn(() => jsonResponse([painting, digitalArt]));

    renderListings();

    expect(await screen.findByText("Sunset over Vilnius")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Ona" })).toHaveAttribute(
      "href",
      "/users/4"
    );
    expect(screen.getByText("Neon river")).toBeInTheDocument();
    expect(
      screen.getByText("Digital art", { selector: "p" })
    ).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "Open listing" })).toHaveLength(
      2
    );
  });

  it("says that nothing matches when filters are set and the list is empty", async () => {
    renderListings("/artworklistings?category=Sculpture");

    expect(
      await screen.findByText("No listings match these filters.")
    ).toBeInTheDocument();
    expect(screen.queryByText(/No listings yet/)).not.toBeInTheDocument();
  });

  it("invites the visitor to sell when there are no listings and no filters", async () => {
    renderListings();

    expect(await screen.findByText(/No listings yet/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Sell your art" })).toHaveAttribute(
      "href",
      "/listings/new"
    );
  });

  it("shows an error and loads the listings again after Try again", async () => {
    const user = userEvent.setup();
    global.fetch = jest
      .fn()
      .mockImplementationOnce(() => jsonResponse(null, 500))
      .mockImplementation(() => jsonResponse([painting]));

    renderListings();

    expect(
      await screen.findByText(/Failed to load listings/)
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Try again" }));

    expect(await screen.findByText("Sunset over Vilnius")).toBeInTheDocument();
    expect(
      screen.queryByText(/Failed to load listings/)
    ).not.toBeInTheDocument();
  });
});
