import React, { useEffect, useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { ListingImage } from "./ListingImage";
import "./Favorites.css";

const pageSize = 12;

export default function Favorites() {
  const { user, isLoading: authLoading } = useAuth();

  const [favorites, setFavorites] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("loading");
  const [error, setError] = useState("");
  const [retryCount, setRetryCount] = useState(0);
  const [removingId, setRemovingId] = useState(null);
  const [actionError, setActionError] = useState("");

  useEffect(() => {
    if (!user) {
      return;
    }

    const controller = new AbortController();

    async function loadFavorites() {
      setStatus("loading");
      setError("");

      try {
        const response = await fetch(
          `/api/users/me/favorites?page=${page}&pageSize=${pageSize}`,
          { signal: controller.signal }
        );

        if (!response.ok) {
          throw new Error("Failed to load favorites.");
        }

        const data = await response.json();

        if (!controller.signal.aborted) {
          setFavorites(data.items ?? []);
          setTotalCount(data.totalCount ?? 0);
          setStatus("success");
        }
      } catch {
        if (!controller.signal.aborted) {
          setError("Unable to load favorites. Please try again.");
          setStatus("error");
        }
      }
    }

    loadFavorites();

    return () => controller.abort();
  }, [user, page, retryCount]);

  async function removeFavorite(artworkId) {
    if (removingId !== null) {
      return;
    }

    setRemovingId(artworkId);
    setActionError("");

    try {
      const response = await fetch(
        `/api/artworklistings/${artworkId}/favorite`,
        { method: "DELETE" }
      );

      if (!response.ok) {
        throw new Error("Failed to remove favorite.");
      }

      setFavorites((current) =>
        current.filter((artwork) => artwork.id !== artworkId)
      );

      setTotalCount((current) => Math.max(0, current - 1));

      if (favorites.length === 1 && page > 1) {
        setPage((current) => current - 1);
      } else {
        setRetryCount((current) => current + 1);
      }
    } catch {
      setActionError(
        "Unable to remove the artwork from favorites. Please try again."
      );
    } finally {
      setRemovingId(null);
    }
  }

  if (authLoading) {
    return <p role="status">Checking your session...</p>;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  const totalPages = Math.ceil(totalCount / pageSize);

  if (status === "loading") {
    return <p role="status">Loading favorites...</p>;
  }

  if (status === "error") {
    return (
      <main className="favorites-page">
        <h1>My favorites</h1>
        <p className="text-danger" role="alert">
          {error}
        </p>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => setRetryCount((count) => count + 1)}
        >
          Try again
        </button>
      </main>
    );
  }

  return (
    <main className="favorites-page">
      <h1>My favorites</h1>
      {actionError && (
        <p className="text-danger" role="alert">
          {actionError}
        </p>
      )}
      {favorites.length === 0 ? (
        <div className="favorites-empty">
          <p>You have not saved any artwork yet.</p>
          <Link to="/artworklistings" className="btn btn-primary">
            Browse artwork
          </Link>
        </div>
      ) : (
        <>
          <div className="favorites-grid">
            {favorites.map((artwork) => {
              const formattedPrice = new Intl.NumberFormat("lt-LT", {
                style: "currency",
                currency: "EUR",
              }).format(artwork.price);

              return (
                <article className="favorite-card" key={artwork.id}>
                  <ListingImage
                    imageUrl={artwork.imageUrl}
                    title={artwork.title}
                  />

                  <h2>{artwork.title}</h2>
                  <p>{artwork.creatorName}</p>
                  <p className="favorite-price">{formattedPrice}</p>

                  <div className="favorite-card-actions">
                    <Link
                      to={`/listings/${artwork.id}`}
                      className="btn btn-primary"
                    >
                      Open artwork
                    </Link>

                    <button
                      type="button"
                      className="btn btn-outline-danger"
                      disabled={removingId !== null}
                      onClick={() => removeFavorite(artwork.id)}
                    >
                      {removingId === artwork.id ? "Removing..." : "Remove"}
                    </button>
                  </div>
                </article>
              );
            })}
          </div>

          {totalPages > 1 && (
            <nav className="favorites-pagination" aria-label="Favorites pages">
              <button
                type="button"
                className="btn btn-outline-secondary"
                disabled={page === 1}
                onClick={() => setPage((current) => current - 1)}
              >
                Previous
              </button>

              <span>
                Page {page} of {totalPages}
              </span>

              <button
                type="button"
                className="btn btn-outline-secondary"
                disabled={page === totalPages}
                onClick={() => setPage((current) => current + 1)}
              >
                Next
              </button>
            </nav>
          )}
        </>
      )}
    </main>
  );
}
