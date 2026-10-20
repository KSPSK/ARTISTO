import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import "./Account.css";

export default function Account() {
  const navigate = useNavigate();
  const { user, isLoading, logout } = useAuth();
  const [error, setError] = useState("");
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  async function handleLogout() {
    setError("");
    setIsLoggingOut(true);

    try {
      await logout();
      navigate("/");
    } catch {
      setError("Unable to log out. Please try again.");
    } finally {
      setIsLoggingOut(false);
    }
  }

  if (isLoading) {
    return <p className="account-page" role="status">Checking your session...</p>;
  }

  if (!user) {
    return (
      <main className="account-page">
        <h1>Account</h1>
        <p className="account-lead">
          Log in to view and edit your creator profile.
        </p>
        <div className="account-actions d-grid gap-2 d-sm-flex">
          <Link className="btn btn-primary" to="/login">Log in</Link>
          <Link className="btn btn-outline-primary" to="/register">Register</Link>
        </div>
      </main>
    );
  }

  return (
    <main className="account-page">
      <h1>Account</h1>
      <p className="account-name">{user.displayName}</p>
      <p className="account-username">@{user.username}</p>
      {error && <div className="alert alert-danger" role="alert">{error}</div>}
      <div className="account-actions d-grid gap-2">
        <Link className="btn btn-primary" to={`/users/${user.id}`}>View profile</Link>
        <Link className="btn btn-outline-primary" to={`/users/${user.id}/edit`}>Edit profile</Link>
        <button
          type="button"
          className="btn btn-outline-secondary"
          onClick={handleLogout}
          disabled={isLoggingOut}
        >
          {isLoggingOut ? "Logging out..." : "Log out"}
        </button>
      </div>
    </main>
  );
}
