import React, { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../AuthContext";
import "./MyOrders.css";

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

export default function MyOrders() {
  const { user, isLoading: authLoading } = useAuth();
  const [role, setRole] = useState("buyer");
  const [orders, setOrders] = useState([]);
  const [status, setStatus] = useState("loading");
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    if (!user) {
      return;
    }

    const controller = new AbortController();
    setStatus("loading");

    fetch(`/api/orders?role=${role}`, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) {
          throw new Error("Failed to load orders.");
        }
        return response.json();
      })
      .then((data) => {
        setOrders(data);
        setStatus("success");
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setStatus("error");
        }
      });

    return () => controller.abort();
  }, [user, role, retryCount]);

  if (authLoading) {
    return <p className="my-orders" role="status">Checking your session...</p>;
  }

  if (!user) {
    return (
      <main className="my-orders">
        <h1>My orders</h1>
        <p className="my-orders-lead">Log in to see your purchases and sales.</p>
        <Link className="btn btn-primary" to="/login">Log in</Link>
      </main>
    );
  }

  function renderContent() {
    if (status === "loading") {
      return <p role="status">Loading orders...</p>;
    }

    if (status === "error") {
      return (
        <div className="alert alert-danger" role="alert">
          <p>Unable to load your orders.</p>
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

    if (orders.length === 0) {
      return (
        <p className="my-orders-empty">
          {role === "buyer" ? (
            <>
              You have not bought anything yet.{" "}
              <Link to="/artworklistings">Explore art</Link>
            </>
          ) : (
            "No one has ordered your artwork yet."
          )}
        </p>
      );
    }

    return (
      <ul className="my-orders-list">
        {orders.map((order) => (
          <li key={order.id} className="my-orders-item">
            <div>
              <Link to={`/orders/${order.id}`} className="my-orders-title">
                {order.artworkTitle}
              </Link>
              <p className="my-orders-meta">
                {role === "buyer"
                  ? `Seller: ${order.sellerDisplayName}, ${order.sellerCity}`
                  : `Buyer: ${order.buyerDisplayName}, ${order.buyerCity}`}
                {" · "}
                {new Date(order.createdAtUtc).toLocaleDateString("lt-LT")}
              </p>
            </div>
            <div className="my-orders-right">
              <span className="my-orders-price">{formatPrice(order.price)}</span>
              <span
                className={`my-orders-status my-orders-status-${order.status.toLowerCase()}`}
              >
                {statusLabels[order.status] || order.status}
              </span>
            </div>
          </li>
        ))}
      </ul>
    );
  }

  return (
    <main className="my-orders">
      <h1>My orders</h1>

      <div className="my-orders-tabs" role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={role === "buyer"}
          className={role === "buyer" ? "active" : ""}
          onClick={() => setRole("buyer")}
        >
          Purchases
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={role === "seller"}
          className={role === "seller" ? "active" : ""}
          onClick={() => setRole("seller")}
        >
          Sales
        </button>
      </div>

      {renderContent()}
    </main>
  );
}