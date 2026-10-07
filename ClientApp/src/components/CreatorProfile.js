import React, { useEffect, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";

function currentUser() {
    try {
        return JSON.parse(sessionStorage.getItem("user") || "null");
    } catch {
        return null;
    }
}

export default function CreatorProfile() {
    const { id } = useParams();
    const location = useLocation();
    const [user, setUser] = useState(null);
    const [listings, setListings] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [notFound, setNotFound] = useState(false);
    const [retryCount, setRetryCount] = useState(0);
    const message = location.state?.message;
    const isOwnProfile = String(currentUser()?.id) === String(id);

    useEffect(() => {
        const controller = new AbortController();

        async function loadProfile() {
            setLoading(true);
            setError("");
            setNotFound(false);
            setUser(null);
            setListings([]);

            try {
                const response = await fetch(`/api/users/${id}`, {
                    signal: controller.signal,
                });

                if (response.status === 404) {
                    setNotFound(true);
                    return;
                }

                if (!response.ok) {
                    throw new Error("Failed to load profile.");
                }

                const profile = await response.json();
                if (controller.signal.aborted) return;

                const listingsResponse = await fetch(`/api/users/${id}/artworklistings`, {
                    signal: controller.signal,
                });

                const profileListings = listingsResponse.ok
                    ? await listingsResponse.json()
                    : [];

                if (!controller.signal.aborted) {
                    setUser(profile);
                    setListings(profileListings);
                }
            } catch (err) {
                if (!controller.signal.aborted) {
                    setError("Unable to load the profile. Please try again.");
                }
            } finally {
                if (!controller.signal.aborted) {
                    setLoading(false);
                }
            }
        }

        loadProfile();

        return () => controller.abort();
    }, [id, retryCount]);

    if (loading) {
        return (
            <div className="listing-details-status" role="status">
                Loading profile...
            </div>
        );
    }

    if (notFound) {
        return (
            <div className="listing-details-status">
                <h1>Profile not found</h1>
                <p>This creator does not exist.</p>
                <Link to="/artworklistings" className="btn btn-secondary">
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
                <button
                    type="button"
                    className="btn btn-primary"
                    onClick={() => setRetryCount((count) => count + 1)}
                >
                    Try again
                </button>
            </div>
        );
    }

    return (
        <main className="listing-details">
            {message && (
                <div className="alert alert-success" role="status">
                    {message}
                </div>
            )}

            <h1>{user.displayName}</h1>
            <p className="text-muted">@{user.username}</p>

            <dl className="listing-details-info">
                <div>
                    <dt>City</dt>
                    <dd>{user.city}</dd>
                </div>
                <div>
                    <dt>Description</dt>
                    <dd>{user.description?.trim() ? user.description : "No description provided."}</dd>
                </div>
            </dl>

            {isOwnProfile && (
                <Link to={`/users/${user.id}/edit`} className="btn btn-primary mb-4">
                    Edit profile
                </Link>
            )}

            <h2>Listings</h2>
            {listings.length === 0 ? (
                <p>No listings yet.</p>
            ) : (
                <div className="artwork-grid">
                    {listings.map((listing) => (
                        <div className="artwork-card" key={listing.id}>
                            <h3 className="artwork-title">{listing.title}</h3>
                            <p className="artwork-price">{listing.price} €</p>
                            <Link className="btn btn-primary artwork-open" to={`/listings/${listing.id}`}>
                                Open listing
                            </Link>
                        </div>
                    ))}
                </div>
            )}
        </main>
    );
}
