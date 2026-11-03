import React, { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { ListingImage } from "./ListingImage";
import "./OrderDetails.css";

const statusLabels = {
  Pending: "Pending",
  HandedOver: "Handed over",
  Completed: "Completed",
  Cancelled: "Cancelled",
};

function formatPrice(price) {
  return new Intl.NumberFormat("lt-LT", {
    style: "currency",
    currency: "EUR",
  }).format(price);
}

function formatDate(value) {
  return new Date(value).toLocaleString("lt-LT");
}

export default function OrderDetails() {
  const { id } = useParams();
  const { user, isLoading: authLoading } = useAuth();
  const [order, setOrder] = useState(null);
  const [status, setStatus] = useState("loading");
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    if (!user) {
      return;
    }

    const controller = new AbortController();
    setStatus("loading");

    async function loadOrder() {
      try {
        const response = await fetch(`/api/orders/${id}`, {
          signal: controller.signal,
        });

        if (response.status === 404) {
          setStatus("notFound");
          return;
        }

        if (response.status === 403) {
          setStatus("forbidden");
          return;
        }

        if (!response.ok) {
          throw new Error("Failed to load order.");
        }

        setOrder(await response.json());
        setStatus("success");
      } catch {
        if (!controller.signal.aborted) {
          setStatus("error");
        }
      }
    }

    loadOrder();

    return () => controller.abort();
  }, [id, user, retryCount]);

  if (authLoading) {
    return <p className="order-details" role="status">Checking your session...</p>;
  }

  if (!user) {
    return (
      <main className="order-details">
        <h1>Order</h1>
        <p className="order-details-muted">Log in to see this order.</p>
        <Link className="btn btn-primary" to="/login">Log in</Link>
      </main>
    );
  }

  if (status === "loading") {
    return <p className="order-details" role="status">Loading order...</p>;
  }

  if (status === "notFound" || status === "forbidden") {
    return (
      <main className="order-details">
        <h1>Order not found</h1>
        <p className="order-details-muted">
          {status === "forbidden"
            ? "You can only view orders where you are the buyer or the seller."
            : "This order does not exist."}
        </p>
        <Link className="btn btn-primary" to="/orders">Back to my orders</Link>
      </main>
    );
  }

  if (status === "error") {
    return (
      <main className="order-details">
        <div className="alert alert-danger" role="alert">
          <p>Unable to load the order.</p>
          <button
            type="button"
            className="btn btn-outline-primary"
            onClick={() => setRetryCount((count) => count + 1)}
          >
            Try again
          </button>
        </div>
      </main>
    );
  }

  const isBuyer = String(order.buyerId) === String(user.id);
  const otherParty = isBuyer
    ? { label: "Seller", id: order.sellerId, name: order.sellerDisplayName, city: order.sellerCity }
    : { label: "Buyer", id: order.buyerId, name: order.buyerDisplayName, city: order.buyerCity };

  return (
    <main className="order-details">
      <Link to="/orders" className="order-details-back">
        ← Back to my orders
      </Link>

      <div className="order-details-header">
        <h1>Order #{order.id}</h1>
        <span
          className={`order-details-status order-details-status-${order.status.toLowerCase()}`}
        >
          {statusLabels[order.status] || order.status}
        </span>
      </div>

      <div className="order-details-grid">
        <ListingImage
          imageUrl={order.artworkImageUrl}
          title={order.artworkTitle}
          large
        />

        <div>
          <h2>
            <Link to={`/listings/${order.artworkListingId}`}>
              {order.artworkTitle}
            </Link>
          </h2>
          <p className="order-details-price">{formatPrice(order.price)}</p>

          <dl className="order-details-info">
            <div>
              <dt>You are the</dt>
              <dd>{isBuyer ? "Buyer" : "Seller"}</dd>
            </div>
            <div>
              <dt>{otherParty.label}</dt>
              <dd>
                <Link to={`/users/${otherParty.id}`}>{otherParty.name}</Link>
                {otherParty.city && `, ${otherParty.city}`}
              </dd>
            </div>
            <div>
              <dt>Pickup notes</dt>
              <dd>{order.pickupNotes || "—"}</dd>
            </div>
            {order.cancellationReason && (
              <div>
                <dt>Cancellation reason</dt>
                <dd>{order.cancellationReason}</dd>
              </div>
            )}
          </dl>

          <h3>Timeline</h3>
          <ul className="order-details-timeline">
            <li>Ordered: {formatDate(order.createdAtUtc)}</li>
            {order.handedOverAtUtc && (
              <li>Handed over: {formatDate(order.handedOverAtUtc)}</li>
            )}
            {order.completedAtUtc && (
              <li>Completed: {formatDate(order.completedAtUtc)}</li>
            )}
            {order.cancelledAtUtc && (
              <li>Cancelled: {formatDate(order.cancelledAtUtc)}</li>
            )}
          </ul>
        </div>
      </div>
    </main>
  );
}