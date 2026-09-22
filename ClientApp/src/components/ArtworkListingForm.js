import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";

const initialForm = {
    title: "",
    description: "",
    price: "",
    creatorName: "",
    category: "Painting",
    localPickupAvailable: false,
};

const categories = [
    ["Painting", "Painting"],
    ["Drawing", "Drawing"],
    ["Photography", "Photography"],
    ["Sculpture", "Sculpture"],
    ["DigitalArt", "Digital art"],
    ["Other", "Other"],
];

export default function ArtworkListingForm() {
    const { id } = useParams();
    const isEditing = Boolean(id);

    const [form, setForm] = useState({ ...initialForm });
    const [success, setSuccess] = useState("");
    const [error, setError] = useState("");
    const [isSaving, setIsSaving] = useState(false);
    const [isLoading, setIsLoading] = useState(Boolean(id));
    const [loadError, setLoadError] = useState("");
    const [loadedId, setLoadedId] = useState(null);
    const [retryCount, setRetryCount] = useState(0);
    const [isDeleting, setIsDeleting] = useState(false);
    const [isDeleted, setIsDeleted] = useState(false);

    // Load the selected listing when the component mounts or when the id changes
    useEffect(() => {
        const controller = new AbortController();

        setSuccess("");
        setError("");
        setLoadError("");
        setLoadedId(null);
        setForm({ ...initialForm });

        if (!id) {
            setIsLoading(false);
            return () => controller.abort();
        }

        setIsLoading(true);

        async function loadListing() {
            try {
                const response = await fetch(`/api/artworklistings/${id}`, {
                    signal: controller.signal,
                });

                if (!response.ok) {
                    throw new Error(
                        response.status === 404
                            ? "Listing not found."
                            : "Failed to load the listing."
                    );
                }

                const listing = await response.json();

                if (controller.signal.aborted) return;

                setForm({
                    title: listing.title,
                    description: listing.description,
                    price: String(listing.price),
                    creatorName: listing.creatorName,
                    category: listing.category,
                    localPickupAvailable: listing.localPickupAvailable,
                });

                setLoadedId(id);
            } catch (err) {
                if (controller.signal.aborted) return;

                setLoadError(
                    err.message || "Failed to connect to the server."
                );
            } finally {
                if (!controller.signal.aborted) {
                    setIsLoading(false);
                }
            }
        }

        loadListing();

        return () => controller.abort();
    }, [id, retryCount]);

    function handleChange(event) {
        const { name, value, type, checked } = event.target;

        setForm((previous) => ({
            ...previous,
            [name]: type === "checkbox" ? checked : value,
        }));

        setIsDeleted(false);
        setSuccess("");
        setError("");
    }

    async function handleDelete() {
        if (isSaving || isDeleting) return;

        const confirmed = window.confirm(
            "Are you sure you want to delete this listing?"
        );

        if (!confirmed) return;

        setSuccess("");
        setError("");
        setIsDeleting(true);

        try {
            const response = await fetch(`/api/artworklistings/${id}`, {
                method: "DELETE",
            });

            if (!response.ok) {
                setError(
                    response.status === 404
                        ? "Listing not found. It may have been deleted."
                        : "Failed to delete the listing. Please try again."
                );
                return;
            }

            setIsDeleted(true);
        } catch {
            setError("Failed to connect to the server. Please try again.");
        } finally {
            setIsDeleting(false);
        }
    }

    async function handleSubmit(event) {
        event.preventDefault();

        if (isSaving || isDeleting) return;

        setSuccess("");
        setError("");
        setIsSaving(true);

        // When editing send the request with the listing ID
        const url = isEditing
            ? `/api/artworklistings/${id}`
            : "/api/artworklistings";

        try {
            const response = await fetch(url, {
                method: isEditing ? "PUT" : "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    ...form,
                    price: Number(form.price),
                }),
            });

            if (!response.ok) {
                setError(
                    response.status === 404
                        ? "Listing not found. It may have been deleted."
                        : "Failed to save the listing. Please try again."
                );
                return;
            }

            setSuccess(
                isEditing
                    ? "Changes saved successfully."
                    : "Listing created successfully."
            );

            // When editing, keep the saved values in the form
            if (!isEditing) {
                setForm({ ...initialForm });
            }
        } catch {
            setError("Failed to connect to the server. Please try again.");
        } finally {
            setIsSaving(false);
        }
    }

    if (isDeleted) {
        return (
            <div className="py-4">
                <div className="alert alert-success" role="status">
                    Listing deleted successfully
                </div>

                <a href="/artworklistings" className="btn btn-primary">
                    Return to listings
                </a>
            </div>
        );
    }

    if (isEditing && loadError) {
        return (
            <div className="py-4">
                <div className="alert alert-danger" role="alert">
                    {loadError}
                </div>

                <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setRetryCount((previous) => previous + 1)}
                >
                    Try again
                </button>
            </div>
        );
    }

    if (isEditing && (isLoading || loadedId !== id)) {
        return (
            <p className="py-4" role="status">
                Listing is loading...
            </p>
        );
    }

    return (
        <div className="mx-auto py-4" style={{ maxWidth: 640 }}>
            <h1>
                {isEditing ? "Edit Listing" : "Create Listing"}
            </h1>

            <p>
                {isEditing
                    ? "Update the artwork information and save."
                    : "Enter details about the artwork you want to sell."}
            </p>

            {success && (
                <div className="alert alert-success" role="status"> 
                    {success}
                </div>
            )}

            {error && (
                <div className="alert alert-danger" role="alert">
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit} noValidate>
                <fieldset disabled={isSaving || isDeleting}>
                    <div className="mb-3">
                        <label htmlFor="title" className="form-label">
                            Title
                        </label>
                        <input
                            id="title"
                            name="title"
                            type="text"
                            className="form-control"
                            value={form.title}
                            onChange={handleChange}
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="creatorName" className="form-label">
                            Creator Name
                        </label>
                        <input
                            id="creatorName"
                            name="creatorName"
                            type="text"
                            className="form-control"
                            value={form.creatorName}
                            onChange={handleChange}
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="description" className="form-label">
                            Description
                        </label>
                        <textarea
                            id="description"
                            name="description"
                            className="form-control"
                            rows={4}
                            value={form.description}
                            onChange={handleChange}
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="price" className="form-label">
                            Price (€)
                        </label>
                        <input
                            id="price"
                            name="price"
                            type="number"
                            step="any"
                            className="form-control"
                            value={form.price}
                            onChange={handleChange}
                        />
                    </div>

                    <div className="mb-3">
                        <label htmlFor="category" className="form-label">
                            Category
                        </label>
                        <select
                            id="category"
                            name="category"
                            className="form-select"
                            value={form.category}
                            onChange={handleChange}
                        >
                            {categories.map(([value, label]) => (
                                <option key={value} value={value}>
                                    {label}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="form-check mb-3">
                        <input
                            id="localPickupAvailable"
                            name="localPickupAvailable"
                            type="checkbox"
                            className="form-check-input"
                            checked={form.localPickupAvailable}
                            onChange={handleChange}
                        />
                        <label
                            htmlFor="localPickupAvailable"
                            className="form-check-label"
                        >
                            Local Pickup Available
                        </label>
                    </div>

                    <button type="submit" className="btn btn-primary">
                        {isSaving
                            ? "Saving..."
                            : isEditing
                                ? "Save changes"
                                : "Create Listing"}
                    </button>
                    {isEditing && (
                        <button
                            type="button"
                            className="btn btn-outline-danger ms-2"
                            onClick={handleDelete}
                        >
                            {isDeleting ? "Deleting..." : "Delete Listing"}
                        </button>
                    )}
                </fieldset>
            </form>
        </div>
    );
}