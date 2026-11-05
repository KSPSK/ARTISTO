import React, { useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { ListingImage } from "./ListingImage";
import {
  artworkCategories,
  artworkListingsApiUrl,
  listingFiltersActive,
  listingSearchPath,
  readListingFilters,
} from "../listingsSearch";
import "./ArtworkListings.css";

export function ArtworkListings() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const location = useLocation();
  const filters = readListingFilters(searchParams);
  const [searchInput, setSearchInput] = useState(filters.q);
  const [categoryInput, setCategoryInput] = useState(filters.category);
  const [maxPriceInput, setMaxPriceInput] = useState(filters.maxPrice);
  const [listings, setListings] = useState([]);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");
  const [retryCount, setRetryCount] = useState(0);
  const message = location.state?.message;
  const filtersActive = listingFiltersActive(filters);

  useEffect(() => {
    setSearchInput(filters.q);
    setCategoryInput(filters.category);
    setMaxPriceInput(filters.maxPrice);
  }, [filters.q, filters.category, filters.maxPrice]);

  useEffect(() => {
    const controller = new AbortController();

    setStatus("loading");
    setErrorMessage("");

    const url = artworkListingsApiUrl(filters);

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
  }, [filters.q, filters.category, filters.maxPrice, retryCount]);

  function handleSearch(event) {
    event.preventDefault();
    navigate(listingSearchPath({
      q: searchInput,
      category: categoryInput,
      maxPrice: maxPriceInput,
    }));
  }

  function handleClear() {
    setSearchInput("");
    setCategoryInput("");
    setMaxPriceInput("");
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

    if (listings.length === 0 && filtersActive) {
      return (
        <p className="artwork-status">No listings match these filters.</p>
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

    const listingsPath = listingSearchPath(filters);

    return (
      <div className="artwork-grid">
        {listings.map((listing) => (
          <div className="artwork-card" key={listing.id}>
            <ListingImage imageUrl={listing.imageUrl} title={listing.title} />
            <h3 className="artwork-title">{listing.title}</h3>
            <p className="artwork-creator">
              {listing.userId ? (
                <Link to={`/users/${listing.userId}`}>{listing.creatorName}</Link>
              ) : (
                listing.creatorName
              )}
            </p>
            <p className="artwork-price">{listing.price} €</p>
            <p className="artwork-category">
              {listing.category === "DigitalArt" ? "Digital art" : listing.category}
            </p>
            <Link
              className="btn btn-primary artwork-open"
              to={`/listings/${listing.id}`}
              state={
                filtersActive
                  ? { fromSearch: filters.q.trim(), listingsPath }
                  : undefined
              }
            >
              Open listing
            </Link>
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="artwork-listings">
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
        <div className="artwork-search-row">
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
        <div className="artwork-filters">
          <div>
            <label className="form-label" htmlFor="listing-category">
              Category
            </label>
            <select
              id="listing-category"
              className="form-select"
              value={categoryInput}
              onChange={(event) => setCategoryInput(event.target.value)}
            >
              <option value="">All categories</option>
              {artworkCategories.map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="form-label" htmlFor="listing-max-price">
              Max price (€)
            </label>
            <input
              id="listing-max-price"
              className="form-control"
              type="number"
              min="0"
              step="any"
              inputMode="decimal"
              value={maxPriceInput}
              onChange={(event) => setMaxPriceInput(event.target.value)}
              placeholder="No maximum"
            />
          </div>
        </div>
      </form>

      {renderContent()}
    </div>
  );
}
