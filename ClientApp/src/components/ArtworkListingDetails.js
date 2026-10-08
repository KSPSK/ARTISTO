import React, { useEffect, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";

const categoryLabels = {
    Painting: "Painting",
    Drawing: "Drawing",
    Photography: "Photography",
    Sculpture: "Sculpture",
    DigitalArt: "Digital art",
    Other: "Other",
};

export default function ArtworkListingDetails() {
    const { id } = useParams();
    const location = useLocation();
    const message = location.state?.message;
    const listingsPath = location.state?.fromSearch
        ? `/artworklistings?q=${encodeURIComponent(location.state.fromSearch)}`
        : "/artworklistings";

    const [listing, setListing] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [notFound, setNotFound] = useState(false);
    const [retryCount, setRetryCount] = useState(0);

    useEffect(() => {
        const controller = new AbortController();

        async function loadListing() {
            setLoading(true);
            setError("");
            setNotFound(false);
            setListing(null);

            try {
                const response = await fetch(`/api/artworklistings/${id}`, {
                    signal: controller.signal,
                });

                if (response.status === 404) {
                    setNotFound(true);
                    return;
                }

                if (!response.ok) {
                    throw new Error("Failed to load listing.");
                }

                const data = await response.json();

                if (!controller.signal.aborted) {
                    setListing(data);
                }
            } catch (err) {
                if (!controller.signal.aborted) {
                    setError("Unable to load the listing. Please try again.");
                }
            } finally {
                if (!controller.signal.aborted) {
                    setLoading(false);
                }
            }
        }

        loadListing();

        return () => controller.abort();
    }, [id, retryCount]);

    if (loading) {
        return (
            <div className="listing-details-status" role="status">
                Loading listing...
            </div>
        );
    }

    if (notFound) {
        return (
            <div className="listing-details-status">
                <h1>Listing not found</h1>
                <p>This listing does not exist or has been removed.</p>

                <Link to={listingsPath} className="btn btn-secondary">
                    Back to listings
                </Link>
            </div>
        );
    }

    if (error) {
        return (
            <div className="listing-details-status">
                <h1>Something went wrong</h1>
                <p role="alert">{error}</p>

                <div className="listing-details-actions">
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={() => setRetryCount((count) => count + 1)}
                    >
                        Try again
                    </button>

                    <Link to={listingsPath} className="btn btn-secondary">
                        Back to listings
                    </Link>
                </div>
            </div>
        );
    }

    if (!listing) {
        return null;
    }

    const formattedPrice = new Intl.NumberFormat("en-IE", {
        style: "currency",
        currency: "EUR",
    }).format(listing.price);

    return (
        <main className="listing-details">
            {message && (
                <div className="alert alert-success" role="status">
                    {message}
                </div>
            )}

            <Link to={listingsPath} className="listing-details-back">
                ← Back to listings
            </Link>

            <div className="listing-details-grid">
                <div className="listing-details-image">
                    <span>No image available</span>
                </div>

                <div className="listing-details-content">
                    <h1>{listing.title}</h1>

                    <p className="listing-details-price">{formattedPrice}</p>

                    <dl className="listing-details-info">
                        <div>
                            <dt>Creator</dt>
                            <dd>
                                {listing.userId ? (
                                    <Link to={`/users/${listing.userId}`}>
                                        {listing.creatorName}
                                    </Link>
                                ) : (
                                    listing.creatorName
                                )}
                            </dd>
                        </div>

                        <div>
                            <dt>Category</dt>
                            <dd>
                                {categoryLabels[listing.category] || listing.category}
                            </dd>
                        </div>

                        <div>
                            <dt>Local pickup</dt>
                            <dd>
                                {listing.localPickupAvailable
                                    ? "Available"
                                    : "Not available"}
                            </dd>
                        </div>
                    </dl>

                    <h2>Description</h2>
                    <p className="listing-details-description">
                        {listing.description?.trim()
                            ? listing.description
                            : "No description provided."}
                    </p>

                    <div className="listing-details-actions">
                        <Link
                            to={`/listings/${listing.id}/edit`}
                            className="btn btn-primary"
                            state={
                                location.state?.fromSearch
                                    ? { fromSearch: location.state.fromSearch }
                                    : undefined
                            }
                        >
                            Edit listing
                        </Link>
                    </div>
                </div>
            </div>
        </main>
    );
}