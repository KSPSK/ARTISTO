import React, { useEffect, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
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
  const location = useLocation();
  const flashMessage = location.state?.message;
  const { user, isLoading: authLoading } = useAuth();
  const [order, setOrder] = useState(null);
  const [status, setStatus] = useState("loading");
  const [retryCount, setRetryCount] = useState(0);
  const [isWorking, setIsWorking] = useState(false);
  const [actionError, setActionError] = useState("");
  const [actionMessage, setActionMessage] = useState("");
  const [showCancel, setShowCancel] = useState(false);
  const [cancelReason, setCancelReason] = useState("");

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

  async function runAction(action, successMessage, body) {
    setIsWorking(true);
    setActionError("");
    setActionMessage("");

    try {
      const response = await fetch(`/api/orders/${id}/${action}`, {
        method: "POST",
        headers: body ? { "Content-Type": "application/json" } : undefined,
        body: body ? JSON.stringify(body) : undefined,
      });

      if (response.status === 403) {
        setActionError("You are not allowed to do this for this order.");
        return;
      }

      if (!response.ok) {
        const problem = await response.json().catch(() => null);
        setActionError(
          problem?.message || "Something went wrong. Please try again."
        );
        return;
      }

      setOrder(await response.json());
      setActionMessage(successMessage);
      setShowCancel(false);
      setCancelReason("");
    } catch {
      setActionError("Unable to connect to the server. Please try again.");
    } finally {
      setIsWorking(false);
    }
  }

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

  const isSeller = !isBuyer;
  const canHandover = isSeller && order.status === "Pending";
  const canConfirmReceipt = isBuyer && order.status === "HandedOver";
  const canCancel = order.status === "Pending";

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

    {flashMessage && !actionMessage && (
        <div className="alert alert-success" role="status">
          {flashMessage}
        </div>
      )}

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
                    {actionMessage && (
            <div className="alert alert-success mt-3" role="status">
              {actionMessage}
            </div>
          )}

          {actionError && (
            <div className="alert alert-danger mt-3" role="alert">
              {actionError}
            </div>
          )}

          {isBuyer && order.status === "Pending" && (
            <p className="order-details-muted mt-3">
              Waiting for the seller to hand over the artwork.
            </p>
          )}

          {isSeller && order.status === "HandedOver" && (
            <p className="order-details-muted mt-3">
              Waiting for the buyer to confirm receipt.
            </p>
          )}

          {(canHandover || canConfirmReceipt || (canCancel && !showCancel)) && (
            <div className="order-details-actions">
              {canHandover && (
                <button
                  type="button"
                  className="btn btn-primary"
                  disabled={isWorking}
                  onClick={() =>
                    runAction(
                      "handover",
                      "Handover confirmed. Waiting for the buyer to confirm receipt."
                    )
                  }
                >
                  Confirm handover
                </button>
              )}

              {canConfirmReceipt && (
                <button
                  type="button"
                  className="btn btn-primary"
                  disabled={isWorking}
                  onClick={() =>
                    runAction("complete", "Receipt confirmed. The order is completed.")
                  }
                >
                  Confirm receipt
                </button>
              )}

              {canCancel && !showCancel && (
                <button
                  type="button"
                  className="btn btn-outline-danger"
                  disabled={isWorking}
                  onClick={() => setShowCancel(true)}
                >
                  Cancel order
                </button>
              )}
            </div>
          )}

          {showCancel && (
            <div className="order-details-cancel">
              <label htmlFor="cancelReason" className="form-label">
                Reason (optional)
              </label>
              <textarea
                id="cancelReason"
                className="form-control mb-2"
                rows={2}
                maxLength={500}
                value={cancelReason}
                onChange={(event) => setCancelReason(event.target.value)}
              />
              <div className="order-details-actions">
                <button
                  type="button"
                  className="btn btn-danger"
                  disabled={isWorking}
                  onClick={() =>
                    runAction(
                      "cancel",
                      "The order was cancelled. The artwork is available again.",
                      { reason: cancelReason.trim() || null }
                    )
                  }
                >
                  Confirm cancellation
                </button>
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  disabled={isWorking}
                  onClick={() => setShowCancel(false)}
                >
                  Keep order
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </main>
  );
}