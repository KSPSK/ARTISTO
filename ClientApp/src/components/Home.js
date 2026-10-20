import React, { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ListingImage } from "./ListingImage";
import "./Home.css";

function formatCategory(category) {
  return category === "DigitalArt" ? "Digital art" : category;
}

export function Home() {
  const [listings, setListings] = useState([]);
  const [status, setStatus] = useState("loading");
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    setStatus("loading");

    async function loadListings() {
      try {
        const response = await fetch("/api/artworklistings", {
          signal: controller.signal,
        });

        if (!response.ok) {
          throw new Error("Failed to load listings.");
        }

        const data = await response.json();
        if (controller.signal.aborted) return;

        setListings(data);
        setStatus("ready");
      } catch {
        if (controller.signal.aborted) return;
        setStatus("error");
      }
    }

    loadListings();

    return () => controller.abort();
  }, [retryCount]);

  const featured = listings.slice(0, 3);

  return (
    <div className="home-page">
      <h1>ARTISTO</h1>
      <p className="home-intro">
        Discover, buy, and sell original artwork in one accessible marketplace.
      </p>

      <div className="home-actions d-grid gap-2 d-sm-flex">
        <Link className="btn btn-primary" to="/artworklistings">
          Explore art
        </Link>
        <Link className="btn btn-outline-primary" to="/listings/new">
          Sell your art
        </Link>
      </div>

      <h2>For buyers</h2>
      <p>
        Browse listings from independent creators and find artwork that matches
        your taste.
      </p>

      <h2>For artists</h2>
      <p>
        Publish a listing with a title, price, and description so buyers can
        find your work.
      </p>

      <h2>Featured listings</h2>

      {status === "loading" && (
        <p role="status">Listings are loading...</p>
      )}

      {status === "error" && (
        <div>
          <div className="alert alert-danger" role="alert">
            Failed to load listings.
          </div>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => setRetryCount((previous) => previous + 1)}
          >
            Try again
          </button>
        </div>
      )}

      {status === "ready" && featured.length === 0 && (
        <p>
          No listings yet.{" "}
          <Link to="/listings/new">Sell your art</Link> to add the first one.
        </p>
      )}

      {status === "ready" && featured.length > 0 && (
        <>
          <div className="artwork-grid">
            {featured.map((listing) => (
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
                  {formatCategory(listing.category)}
                </p>
                <Link
                  className="btn btn-primary artwork-open"
                  to={`/listings/${listing.id}`}
                >
                  Open listing
                </Link>
              </div>
            ))}
          </div>
          <p className="home-more">
            <Link to="/artworklistings">See all listings</Link>
          </p>
        </>
      )}
    </div>
  );
}
