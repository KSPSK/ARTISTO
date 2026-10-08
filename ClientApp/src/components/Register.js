import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";

async function readError(response, fallback) {
    const body = await response.json().catch(() => null);

    if (body?.message) {
        return body.message;
    }

    if (body?.errors) {
        return Object.values(body.errors).flat().join(" ");
    }

    return fallback;
}

export default function Register() {
    const navigate = useNavigate();
    const [form, setForm] = useState({
        username: "",
        password: "",
        displayName: "",
        description: "",
        city: "",
    });
    const [error, setError] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    function handleChange(event) {
        const { name, value } = event.target;
        setForm((previous) => ({ ...previous, [name]: value }));
    }

    async function handleSubmit(event) {
        event.preventDefault();

        if (isLoading) return;

        setError("");
        setIsLoading(true);

        try {
            const response = await fetch("/api/users/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    username: form.username.trim(),
                    password: form.password,
                    displayName: form.displayName.trim(),
                    description: form.description.trim(),
                    city: form.city.trim(),
                }),
            });

            if (!response.ok) {
                setError(await readError(response, "Unable to register. Please try again."));
                return;
            }

            const user = await response.json();
            sessionStorage.setItem("user", JSON.stringify(user));

            navigate(`/users/${user.id}`, {
                replace: true,
                state: { message: "Account created." },
            });
        } catch {
            setError("Unable to connect to the server. Please try again.");
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <main className="mx-auto py-5" style={{ maxWidth: 480 }}>
            <h1 className="mb-2">Create account</h1>
            <p className="text-muted mb-4">
                Register a creator profile on ARTISTO.
            </p>

            {error && (
                <div className="alert alert-danger" role="alert">
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit}>
                <fieldset disabled={isLoading}>
                    <div className="mb-3">
                        <label htmlFor="username" className="form-label">Username</label>
                        <input
                            id="username"
                            name="username"
                            type="text"
                            className="form-control"
                            autoComplete="username"
                            value={form.username}
                            onChange={handleChange}
                            required
                            autoFocus
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="password" className="form-label">Password</label>
                        <input
                            id="password"
                            name="password"
                            type="password"
                            className="form-control"
                            autoComplete="new-password"
                            value={form.password}
                            onChange={handleChange}
                            required
                            minLength={4}
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="displayName" className="form-label">Display name</label>
                        <input
                            id="displayName"
                            name="displayName"
                            type="text"
                            className="form-control"
                            value={form.displayName}
                            onChange={handleChange}
                            required
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="city" className="form-label">City</label>
                        <input
                            id="city"
                            name="city"
                            type="text"
                            className="form-control"
                            value={form.city}
                            onChange={handleChange}
                            required
                        />
                    </div>

                    <div className="mb-4">
                        <label htmlFor="description" className="form-label">Description</label>
                        <textarea
                            id="description"
                            name="description"
                            className="form-control"
                            rows={4}
                            value={form.description}
                            onChange={handleChange}
                        />
                    </div>

                    <button type="submit" className="btn btn-primary w-100">
                        {isLoading ? "Creating account..." : "Create account"}
                    </button>
                </fieldset>
            </form>

            <p className="mt-3 mb-0">
                Already have an account? <Link to="/login">Log in</Link>
            </p>
        </main>
    );
}
