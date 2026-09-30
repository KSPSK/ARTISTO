import React, { useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";

export function ArtworkListings() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const location = useLocation();
  const searchQuery = searchParams.get("q") ?? "";
  const [searchInput, setSearchInput] = useState(searchQuery);
  const [listings, setListings] = useState([]);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");
  const [retryCount, setRetryCount] = useState(0);
  const message = location.state?.message;

  useEffect(() => {
    setSearchInput(searchQuery);
  }, [searchQuery]);

  useEffect(() => {
    const controller = new AbortController();

    setStatus("loading");
    setErrorMessage("");

    const query = searchQuery.trim();
    const url = query
      ? `/api/artworklistings?q=${encodeURIComponent(query)}`
      : "/api/artworklistings";

    fetch(url, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Server responded with an error (${response.status})`);
        }
        return response.json();
      })
      .then((data) => {
        if (controller.signal.aborted) return;
        setListings(data);
        setStatus("success");
      })
      .catch((error) => {
        if (controller.signal.aborted) return;
        setStatus("error");
        setErrorMessage(error.message);
      });

    return () => controller.abort();
  }, [searchQuery, retryCount]);

  function handleSearch(event) {
    event.preventDefault();
    const nextQuery = searchInput.trim();

    navigate(
      nextQuery
        ? `/artworklistings?q=${encodeURIComponent(nextQuery)}`
        : "/artworklistings"
    );
  }

  function handleClear() {
    setSearchInput("");
    navigate("/artworklistings");
  }

  function renderContent() {
    if (status === "loading") {
      return <p>Loading listings...</p>;
    }

    if (status === "error") {
      return (
        <div className="artwork-status artwork-status-error">
          <p>Failed to load listings. {errorMessage}.</p>
          <button
            className="btn btn-secondary"
            type="button"
            onClick={() => setRetryCount((previous) => previous + 1)}
          >
            Try again
          </button>
        </div>
      );
    }

    if (listings.length === 0 && searchQuery.trim()) {
      return (
        <p className="artwork-status">No listings match your search.</p>
      );
    }

    if (listings.length === 0) {
      return (
        <p className="artwork-status">
          No listings yet. <Link to="/listings/new">Sell your art</Link> to add
          the first one.
        </p>
      );
    }

    const query = searchQuery.trim();

    return (
      <div className="artwork-grid">
        {listings.map((listing) => (
          <div className="artwork-card" key={listing.id}>
            <h3 className="artwork-title">{listing.title}</h3>
            <p className="artwork-creator">{listing.creatorName}</p>
            <p className="artwork-price">{listing.price} €</p>
            <p className="artwork-category">
              {listing.category === "DigitalArt" ? "Digital art" : listing.category}
            </p>
            <Link
              className="btn btn-primary artwork-open"
              to={`/listings/${listing.id}`}
              state={query ? { fromSearch: query } : undefined}
            >
              Open listing
            </Link>
          </div>
        ))}
      </div>
    );
  }

  return (
    <div>
      <h1>Artwork Listings</h1>

      {message && (
        <div className="alert alert-success" role="status">
          {message}
        </div>
      )}

      <form className="artwork-search" onSubmit={handleSearch}>
        <label className="form-label" htmlFor="listing-search">
          Search
        </label>
        <div className="d-grid gap-2 d-sm-flex">
          <input
            id="listing-search"
            className="form-control"
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Title or creator name"
          />
          <button className="btn btn-primary" type="submit">
            Search
          </button>
          <button className="btn btn-outline-secondary" type="button" onClick={handleClear}>
            Clear
          </button>
        </div>
      </form>

      {renderContent()}
    </div>
  );
}
