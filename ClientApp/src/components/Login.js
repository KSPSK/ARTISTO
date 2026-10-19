import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import "./Login.css";
import logo from "../images/artisto_logo.png";

export default function Login() {
  const navigate = useNavigate();
  const { isLoading: authLoading, setUser } = useAuth();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();

    if (isLoading) return;

    setError("");
    setIsLoading(true);

    try {
      const response = await fetch("/api/users/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          username: username.trim(),
          password,
        }),
      });

      if (!response.ok) {
        if (response.status === 401) {
          setError("Invalid username or password.");
        } else {
          setError("Unable to log in. Please try again.");
        }

        return;
      }

      const user = await response.json();

      setUser(user);

      navigate("/artworklistings", {
        replace: true,
        state: {
          message: "Logged in successfully.",
        },
      });
    } catch {
      setError("Unable to connect to the server. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  if (authLoading) {
    return <p className="py-4" role="status">Checking your session...</p>;
  }

  return (
    <section className="login-page">
      <img
      src={logo}
      alt="ARTISTO"
      className="login-logo"
      />
      <p className="text-muted mb-4">
        LOG IN TO YOUR ARTISTO ACCOUNT.
      </p>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <fieldset disabled={isLoading}>
          <div className="mb-3">
            <label htmlFor="username" className="form-label">
              Username
            </label>

            <input
              id="username"
              name="username"
              type="text"
              placeholder="Enter your username"
              className="form-control"
              autoComplete="username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              required
              autoFocus
            />
          </div>

          <div className="mb-4">
            <label htmlFor="password" className="form-label">
              Password
            </label>

            <input
              id="password"
              name="password"
              type="password"
              placeholder="Password"
              className="form-control"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary w-100"
          >
            {isLoading ? "Logging in..." : "LOG IN"}
          </button>
        </fieldset>
      </form>

      <div className="login-links">
        <p>
          No account yet? <Link to="/register">Create one</Link>
        </p>

        <Link to="/" className="login-back">
          Back to home
        </Link>
      </div>
    </section>
  );
}
