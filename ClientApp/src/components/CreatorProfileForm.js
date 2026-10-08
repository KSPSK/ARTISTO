import React, { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";

export default function CreatorProfileForm() {
    const { id } = useParams();
    const navigate = useNavigate();
    const [form, setForm] = useState({
        displayName: "",
        description: "",
        city: "",
    });
    const [loading, setLoading] = useState(true);
    const [notFound, setNotFound] = useState(false);
    const [error, setError] = useState("");
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        const controller = new AbortController();

        async function loadProfile() {
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

                const user = await response.json();
                if (controller.signal.aborted) return;

                setForm({
                    displayName: user.displayName,
                    description: user.description,
                    city: user.city,
                });
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
    }, [id]);

    function handleChange(event) {
        const { name, value } = event.target;
        setForm((previous) => ({ ...previous, [name]: value }));
        setError("");
    }

    async function handleSubmit(event) {
        event.preventDefault();

        if (isSaving) return;

        setError("");
        setIsSaving(true);

        const updated = {
            displayName: form.displayName.trim(),
            description: form.description.trim(),
            city: form.city.trim(),
        };

        try {
            const response = await fetch(`/api/users/${id}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(updated),
            });

            if (!response.ok) {
                const body = await response.json().catch(() => null);
                const message = body?.message
                    || (body?.errors ? Object.values(body.errors).flat().join(" ") : "");
                setError(message || "Unable to save the profile. Please try again.");
                return;
            }

            const saved = await response.json();
            const stored = JSON.parse(sessionStorage.getItem("user") || "null");

            if (stored && String(stored.id) === String(saved.id)) {
                sessionStorage.setItem("user", JSON.stringify(saved));
            }

            navigate(`/users/${saved.id}`, {
                state: { message: "Profile updated." },
            });
        } catch {
            setError("Unable to connect to the server. Please try again.");
        } finally {
            setIsSaving(false);
        }
    }

    if (loading) {
        return (
            <p className="py-4" role="status">
                Loading profile...
            </p>
        );
    }

    if (notFound) {
        return (
            <div className="py-4">
                <p>Profile not found.</p>
                <Link to="/artworklistings">Back to listings</Link>
            </div>
        );
    }

    return (
        <div className="mx-auto py-4" style={{ maxWidth: 640 }}>
            <h1>Edit profile</h1>
            <p>Update the display name, city, and description.</p>

            {error && (
                <div className="alert alert-danger" role="alert">
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit}>
                <fieldset disabled={isSaving}>
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

                    <div className="mb-3">
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

                    <button type="submit" className="btn btn-primary">
                        {isSaving ? "Saving..." : "Save profile"}
                    </button>
                    <Link to={`/users/${id}`} className="btn btn-outline-secondary ms-2">
                        Cancel
                    </Link>
                </fieldset>
            </form>
        </div>
    );
}
