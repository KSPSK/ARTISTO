import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";

export default function Login() {
    const navigate = useNavigate();

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

            sessionStorage.setItem("user", JSON.stringify(user));

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

    return (
        <main className="mx-auto py-5" style={{ maxWidth: 400 }}>
            <h1 className="mb-2">Log in</h1>
            <p className="text-muted mb-4">
                Log in to your ARTISTO account.
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
                        {isLoading ? "Logging in..." : "Log in"}
                    </button>
                </fieldset>
            </form>

            <p className="mt-3 mb-0">
                No account yet? <Link to="/register">Create one</Link>
            </p>

            <Link to="/" className="d-inline-block mt-3">
                Back to home
            </Link>
        </main>
    );
}