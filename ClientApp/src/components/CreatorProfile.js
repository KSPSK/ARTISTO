import React, { useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { ListingImage } from "./ListingImage";
import { useAuth } from "../AuthContext";
import "./CreatorProfile.css";

export default function CreatorProfile() {
  const { id } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { user: currentUser, isLoading: authLoading } = useAuth();
  const [user, setUser] = useState(null);
  const [listings, setListings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [notFound, setNotFound] = useState(false);
  const [retryCount, setRetryCount] = useState(0);
  const [followLoading, setFollowLoading] = useState(false);
  const [followError, setFollowError] = useState("");
  const message = location.state?.message;
  const isOwnProfile = String(currentUser?.id) === String(id);

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

  async function toggleFollow() {
    if (authLoading || followLoading || !user || isOwnProfile) {
      return;
    }

    if (!currentUser) {
      navigate("/login");
      return;
    }

    const shouldFollow = !user.isFollowing;

    setFollowLoading(true);
    setFollowError("");

    try {
      const response = await fetch(`/api/users/${user.id}/follow`, {
        method: shouldFollow ? "PUT" : "DELETE",
      });

      if (response.status === 401) {
        navigate("/login");
        return;
      }

      if (!response.ok) {
        throw new Error("Failed to update follow status.");
      }

      setUser((current) => ({
        ...current,
        isFollowing: shouldFollow,
        followerCount: Math.max(
          0,
          (current.followerCount ?? 0) + (shouldFollow ? 1 : -1)
        ),
      }));
    } catch {
      setFollowError("Unable to update follow status. Please try again.");
    } finally {
      setFollowLoading(false);
    }
  }

  if (loading) {
    return (
      <div className="creator-profile listing-details-status" role="status">
        Loading profile...
      </div>
    );
  }

  if (notFound) {
    return (
      <div className="creator-profile listing-details-status">
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
      <div className="creator-profile listing-details-status">
        <h1>Something went wrong</h1>
        <p role="alert">{error}</p>
        <button
          type="button"
          className="btn btn-primary creator-retry"
          onClick={() => setRetryCount((count) => count + 1)}
        >
          Try again
        </button>
      </div>
    );
  }

  return (
    <main className="creator-profile listing-details">
      {message && (
        <div className="alert alert-success" role="status">
          {message}
        </div>
      )}

      <h1>{user.displayName}</h1>
      <p className="creator-username">@{user.username}</p>

      <div className="creator-follow-summary">
        <span>
          <strong>{user.followerCount ?? 0}</strong> followers
        </span>

        <span>
          <strong>{user.followingCount ?? 0}</strong> following
        </span>
      </div>

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

      <div className="creator-actions">
        {isOwnProfile ? (
          <Link
            to={`/users/${user.id}/edit`}
            className="btn btn-primary creator-edit"
          >
            Edit profile
          </Link>
        ) : (
          <button
            type="button"
            className={`btn ${user.isFollowing ? "btn-secondary" : "btn-primary"
              } creator-follow-action`}
            disabled={authLoading || followLoading}
            onClick={toggleFollow}
          >
            {followLoading
              ? "Updating..."
              : user.isFollowing
                ? "Unfollow"
                : "Follow"}
          </button>
        )}
      </div>

      {followError && (
        <p className="creator-follow-error" role="alert">
          {followError}
        </p>
      )}

      <h2>Listings</h2>
      {listings.length === 0 ? (
        <p>No listings yet.</p>
      ) : (
        <div className="artwork-grid">
          {listings.map((listing) => (
            <div className="artwork-card" key={listing.id}>
              <ListingImage imageUrl={listing.imageUrl} title={listing.title} />
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
