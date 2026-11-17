import React, { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../AuthContext";
import "./MyOffers.css";

const statusLabels = {
  Pending: "Pending",
  Countered: "Counter-offer",
  Accepted: "Accepted",
  Declined: "Declined",
};

function formatPrice(price) {
  return new Intl.NumberFormat("lt-LT", {
    style: "currency",
    currency: "EUR",
  }).format(price);
}

function OfferItem({ offer, role, onChanged }) {
  const [isWorking, setIsWorking] = useState(false);
  const [error, setError] = useState("");
  const [showCounter, setShowCounter] = useState(false);
  const [counterAmount, setCounterAmount] = useState("");
  const [counterMessage, setCounterMessage] = useState("");

  const isSeller = role === "seller";
  const canSellerRespond = isSeller && offer.status === "Pending";
  const canBuyerRespond = !isSeller && offer.status === "Countered";

  async function respond(action, body) {
    setIsWorking(true);
    setError("");

    try {
      const response = await fetch(`/api/offers/${offer.id}/${action}`, {
        method: "POST",
        headers: body ? { "Content-Type": "application/json" } : undefined,
        body: body ? JSON.stringify(body) : undefined,
      });

      if (response.status === 403) {
        setError("You are not allowed to do this for this offer.");
        return;
      }

      if (!response.ok) {
        const problem = await response.json().catch(() => null);
        setError(problem?.message || "Something went wrong. Please try again.");
        return;
      }

      setShowCounter(false);
      onChanged(await response.json(), action);
    } catch {
      setError("Unable to connect to the server. Please try again.");
    } finally {
      setIsWorking(false);
    }
  }

  function handleCounterSubmit(event) {
    event.preventDefault();
    respond("counter", {
      amount: Number(counterAmount),
      message: counterMessage.trim() || null,
    });
  }

  return (
    <li className="my-offers-item">
      <div className="my-offers-row">
        <div>
          <Link to={`/listings/${offer.artworkListingId}`} className="my-offers-title">
            {offer.artworkTitle}
          </Link>
          <p className="my-offers-meta">
            {isSeller
              ? `From: ${offer.buyerDisplayName}`
              : `Seller: ${offer.sellerDisplayName}`}
            {" · "}
            Listed at {formatPrice(offer.listingPrice)}
            {" · "}
            {new Date(offer.createdAtUtc).toLocaleDateString("lt-LT")}
          </p>
        </div>
        <div className="my-offers-right">
          <span className="my-offers-price">{formatPrice(offer.amount)}</span>
          <span
            className={`my-offers-status my-offers-status-${offer.status.toLowerCase()}`}
          >
            {statusLabels[offer.status] || offer.status}
          </span>
        </div>
      </div>

      {offer.message && (
        <p className="my-offers-message">"{offer.message}"</p>
      )}

      {offer.counterAmount != null && (
        <p className="my-offers-counter">
          Counter-offer: <strong>{formatPrice(offer.counterAmount)}</strong>
          {offer.counterMessage && <> · "{offer.counterMessage}"</>}
        </p>
      )}

      {offer.status === "Accepted" && offer.orderId && (
        <Link className="btn btn-outline-primary btn-sm" to={`/orders/${offer.orderId}`}>
          View order
        </Link>
      )}

      {canSellerRespond && !showCounter && (
        <div className="my-offers-actions">
          <button
            type="button"
            className="btn btn-primary btn-sm"
            disabled={isWorking}
            onClick={() => respond("accept")}
          >
            Accept
          </button>
          <button
            type="button"
            className="btn btn-outline-primary btn-sm"
            disabled={isWorking}
            onClick={() => setShowCounter(true)}
          >
            Counter
          </button>
          <button
            type="button"
            className="btn btn-outline-danger btn-sm"
            disabled={isWorking}
            onClick={() => respond("decline")}
          >
            Decline
          </button>
        </div>
      )}

      {canSellerRespond && showCounter && (
        <form className="my-offers-counter-form" onSubmit={handleCounterSubmit}>
          <label htmlFor={`counter-amount-${offer.id}`}>Your price (EUR)</label>
          <input
            id={`counter-amount-${offer.id}`}
            type="number"
            className="form-control"
            min={(offer.amount + 0.01).toFixed(2)}
            max={offer.listingPrice}
            step="0.01"
            required
            value={counterAmount}
            onChange={(event) => setCounterAmount(event.target.value)}
          />
          <label htmlFor={`counter-message-${offer.id}`}>Message (optional)</label>
          <textarea
            id={`counter-message-${offer.id}`}
            className="form-control"
            maxLength={1000}
            rows={2}
            value={counterMessage}
            onChange={(event) => setCounterMessage(event.target.value)}
          />
          <div className="my-offers-actions">
            <button type="submit" className="btn btn-primary btn-sm" disabled={isWorking}>
              Send counter-offer
            </button>
            <button
              type="button"
              className="btn btn-link btn-sm"
              disabled={isWorking}
              onClick={() => setShowCounter(false)}
            >
              Cancel
            </button>
          </div>
        </form>
      )}

      {canBuyerRespond && (
        <div className="my-offers-actions">
          <button
            type="button"
            className="btn btn-primary btn-sm"
            disabled={isWorking}
            onClick={() => respond("accept")}
          >
            Accept {formatPrice(offer.counterAmount)}
          </button>
          <button
            type="button"
            className="btn btn-outline-danger btn-sm"
            disabled={isWorking}
            onClick={() => respond("decline")}
          >
            Decline
          </button>
        </div>
      )}

      {error && (
        <p className="my-offers-error" role="alert">{error}</p>
      )}
    </li>
  );
}

export default function MyOffers() {
  const { user, isLoading: authLoading } = useAuth();
  const [role, setRole] = useState("seller");
  const [offers, setOffers] = useState([]);
  const [status, setStatus] = useState("loading");
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    if (!user) {
      return;
    }

    const controller = new AbortController();
    setStatus("loading");

    fetch(`/api/offers?role=${role}`, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) {
          throw new Error("Failed to load offers.");
        }
        return response.json();
      })
      .then((data) => {
        setOffers(data);
        setStatus("success");
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setStatus("error");
        }
      });

    return () => controller.abort();
  }, [user, role, retryCount]);

  function handleChanged(updated, action) {
    if (action === "accept") {
      // Accepting declines the other open offers, so reload the whole list.
      setRetryCount((count) => count + 1);
      return;
    }

    setOffers((current) =>
      current.map((offer) => (offer.id === updated.id ? updated : offer))
    );
  }

  if (authLoading) {
    return <p className="my-offers" role="status">Checking your session...</p>;
  }

  if (!user) {
    return (
      <main className="my-offers">
        <h1>My offers</h1>
        <p className="my-offers-lead">Log in to see your offers.</p>
        <Link className="btn btn-primary" to="/login">Log in</Link>
      </main>
    );
  }

  function renderContent() {
    if (status === "loading") {
      return <p role="status">Loading offers...</p>;
    }

    if (status === "error") {
      return (
        <div className="alert alert-danger" role="alert">
          <p>Unable to load your offers.</p>
          <button
            type="button"
            className="btn btn-outline-primary"
            onClick={() => setRetryCount((count) => count + 1)}
          >
            Try again
          </button>
        </div>
      );
    }

    if (offers.length === 0) {
      return (
        <p className="my-offers-empty">
          {role === "seller" ? (
            "No one has made an offer on your artwork yet."
          ) : (
            <>
              You have not made any offers yet.{" "}
              <Link to="/artworklistings">Explore art</Link>
            </>
          )}
        </p>
      );
    }

    return (
      <ul className="my-offers-list">
        {offers.map((offer) => (
          <OfferItem
            key={offer.id}
            offer={offer}
            role={role}
            onChanged={handleChanged}
          />
        ))}
      </ul>
    );
  }

  return (
    <main className="my-offers">
      <h1>My offers</h1>

      <div className="my-offers-tabs" role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={role === "seller"}
          className={role === "seller" ? "active" : ""}
          onClick={() => setRole("seller")}
        >
          Received
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={role === "buyer"}
          className={role === "buyer" ? "active" : ""}
          onClick={() => setRole("buyer")}
        >
          Sent
        </button>
      </div>

      {renderContent()}
    </main>
  );
}