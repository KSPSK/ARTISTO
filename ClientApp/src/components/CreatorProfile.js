import React, { useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { ListingImage } from "./ListingImage";
import { useAuth } from "../AuthContext";
import "./CreatorProfile.css";

const connectionsPageSize = 20;
const connectionLabels = {
  followers: "Followers",
  following: "Following",
};

export default function CreatorProfile() {
  const { id } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { user: currentUser } = useAuth();
  const [user, setUser] = useState(null);
  const [listings, setListings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [notFound, setNotFound] = useState(false);
  const [retryCount, setRetryCount] = useState(0);
  const [isFollowUpdating, setIsFollowUpdating] = useState(false);
  const [followError, setFollowError] = useState("");
  const [connectionsView, setConnectionsView] = useState(null);
  const [connections, setConnections] = useState([]);
  const [connectionsTotal, setConnectionsTotal] = useState(0);
  const [connectionsPage, setConnectionsPage] = useState(1);
  const [connectionsStatus, setConnectionsStatus] = useState("idle");
  const [connectionsRetryCount, setConnectionsRetryCount] = useState(0);
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
      setFollowError("");
      setConnectionsView(null);

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

  useEffect(() => {
    if (!connectionsView) {
      return;
    }

    const controller = new AbortController();

    async function loadConnections() {
      setConnectionsStatus("loading");

      try {
        const response = await fetch(
          `/api/users/${id}/${connectionsView}?page=${connectionsPage}&pageSize=${connectionsPageSize}`,
          { signal: controller.signal }
        );

        if (!response.ok) {
          throw new Error(`Failed to load ${connectionsView}.`);
        }

        const data = await response.json();

        if (!controller.signal.aborted) {
          setConnections(data.items ?? []);
          setConnectionsTotal(data.totalCount ?? 0);
          setConnectionsStatus("success");
        }
      } catch {
        if (!controller.signal.aborted) {
          setConnectionsStatus("error");
        }
      }
    }

    loadConnections();

    return () => controller.abort();
  }, [id, connectionsView, connectionsPage, connectionsRetryCount]);

  function showConnections(view) {
    setConnectionsView((current) => (current === view ? null : view));
    setConnectionsPage(1);
    setConnections([]);
    setConnectionsStatus("loading");
  }

  async function toggleFollow() {
    if (!currentUser) {
      navigate("/login");
      return;
    }

    const isFollowing = user.isFollowing;

    setIsFollowUpdating(true);
    setFollowError("");

    try {
      const response = await fetch(`/api/users/${user.id}/follow`, {
        method: isFollowing ? "DELETE" : "PUT",
      });

      if (response.status === 401) {
        navigate("/login");
        return;
      }

      if (!response.ok) {
        throw new Error("Unable to update follow.");
      }

      setUser((current) => ({
        ...current,
        isFollowing: !isFollowing,
        followerCount: Math.max(
          0,
          (current.followerCount ?? 0) + (isFollowing ? -1 : 1)
        ),
      }));

      if (connectionsView === "followers") {
        setConnectionsRetryCount((count) => count + 1);
      }
    } catch {
      setFollowError("Unable to update follow. Please try again.");
    } finally {
      setIsFollowUpdating(false);
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

      <div className="creator-social">
        <button
          type="button"
          className="creator-count"
          aria-pressed={connectionsView === "followers"}
          onClick={() => showConnections("followers")}
        >
          <strong>{user.followerCount ?? 0}</strong>{" "}
          {user.followerCount === 1 ? "follower" : "followers"}
        </button>

        <button
          type="button"
          className="creator-count"
          aria-pressed={connectionsView === "following"}
          onClick={() => showConnections("following")}
        >
          <strong>{user.followingCount ?? 0}</strong> following
        </button>

        {!isOwnProfile && (
          <button
            type="button"
            className={`btn creator-follow ${
              user.isFollowing ? "btn-secondary" : "btn-primary"
            }`}
            disabled={isFollowUpdating}
            onClick={toggleFollow}
          >
            {user.isFollowing ? "Unfollow" : "Follow"}
          </button>
        )}
      </div>

      {followError && (
        <p className="text-danger" role="alert">
          {followError}
        </p>
      )}

      {connectionsView && (
        <section className="creator-connections">
          <h2>{connectionLabels[connectionsView]}</h2>

          {connectionsStatus === "loading" && (
            <p role="status">Loading {connectionsView}...</p>
          )}

          {connectionsStatus === "error" && (
            <>
              <p className="text-danger" role="alert">
                Unable to load {connectionsView}. Please try again.
              </p>
              <button
                type="button"
                className="btn btn-primary creator-retry"
                onClick={() => setConnectionsRetryCount((count) => count + 1)}
              >
                Try again
              </button>
            </>
          )}

          {connectionsStatus === "success" && connections.length === 0 && (
            <p>
              {connectionsView === "followers"
                ? "No followers yet."
                : "Not following anyone yet."}
            </p>
          )}

          {connectionsStatus === "success" && connections.length > 0 && (
            <>
              <ul className="creator-connections-list">
                {connections.map((connection) => (
                  <li key={connection.id}>
                    <Link to={`/users/${connection.id}`}>
                      {connection.displayName}
                    </Link>
                    <span className="creator-username">
                      @{connection.username}
                    </span>
                  </li>
                ))}
              </ul>

              {connectionsTotal > connectionsPageSize && (
                <nav
                  className="creator-connections-pagination"
                  aria-label={`${connectionLabels[connectionsView]} pages`}
                >
                  <button
                    type="button"
                    className="btn btn-secondary"
                    disabled={connectionsPage === 1}
                    onClick={() => setConnectionsPage((page) => page - 1)}
                  >
                    Previous
                  </button>

                  <span>
                    Page {connectionsPage} of{" "}
                    {Math.ceil(connectionsTotal / connectionsPageSize)}
                  </span>

                  <button
                    type="button"
                    className="btn btn-secondary"
                    disabled={
                      connectionsPage * connectionsPageSize >= connectionsTotal
                    }
                    onClick={() => setConnectionsPage((page) => page + 1)}
                  >
                    Next
                  </button>
                </nav>
              )}
            </>
          )}
        </section>
      )}

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
        <Link to={`/users/${user.id}/edit`} className="btn btn-primary creator-edit">
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
