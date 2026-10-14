import React, { useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../AuthContext";

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
  const navigate = useNavigate();
  const location = useLocation();
  const { user, isLoading: authLoading, setUser } = useAuth();
  const isEditing = Boolean(id);
  const fromSearch = location.state?.fromSearch;

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
  const [ownerId, setOwnerId] = useState(null);
  const [persistedId, setPersistedId] = useState(id || null);
  const [selectedImage, setSelectedImage] = useState(null);
  const [previewUrl, setPreviewUrl] = useState("");
  const [currentImageUrl, setCurrentImageUrl] = useState("");
  const [removeCurrentImage, setRemoveCurrentImage] = useState(false);
  const [imageError, setImageError] = useState(location.state?.imageError || "");
  const [isUploading, setIsUploading] = useState(false);

  // Load the selected listing when the component mounts or when the id changes
  useEffect(() => {
    const controller = new AbortController();

    setSuccess("");
    setError("");
    setLoadError("");
    setLoadedId(null);
    setOwnerId(null);
    setPersistedId(id || null);
    setSelectedImage(null);
    setPreviewUrl("");
    setCurrentImageUrl("");
    setRemoveCurrentImage(false);
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
        setOwnerId(listing.userId ?? null);
        setCurrentImageUrl(listing.imageUrl || "");
        setPersistedId(listing.id);

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
    setImageError("");
  }

  function handleImageChange(event) {
    const file = event.target.files?.[0];
    setImageError("");
    setRemoveCurrentImage(false);

    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }

    if (!file) {
      setSelectedImage(null);
      setPreviewUrl("");
      return;
    }

    const allowed = ["image/jpeg", "image/png", "image/webp"];
    if (!allowed.includes(file.type) || file.size > 5 * 1024 * 1024) {
      setSelectedImage(null);
      setPreviewUrl("");
      setImageError(
        file.size > 5 * 1024 * 1024
          ? "The image must be 5 MB or smaller."
          : "Choose a JPEG, PNG, or WebP image."
      );
      event.target.value = "";
      return;
    }

    setSelectedImage(file);
    setPreviewUrl(URL.createObjectURL(file));
  }

  async function handleDelete() {
    if (isSaving || isDeleting || isUploading) return;

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
        if (response.status === 401) {
          setUser(null);
          setError("Your session has expired. Please log in again.");
          return;
        }

        if (response.status === 403) {
          setError("You can only delete your own listings.");
          return;
        }

        setError(
          response.status === 404
            ? "Listing not found. It may have been deleted."
            : "Failed to delete the listing. Please try again."
        );
        return;
      }

      setIsDeleted(true);
      navigate(
        fromSearch
          ? `/artworklistings?q=${encodeURIComponent(fromSearch)}`
          : "/artworklistings",
        { state: { message: "Listing deleted." } }
      );
    } catch {
      setError("Failed to connect to the server. Please try again.");
    } finally {
      setIsDeleting(false);
    }
  }

  async function handleSubmit(event) {
    event.preventDefault();

    if (isSaving || isDeleting || isUploading) return;

    setSuccess("");
    setError("");
    setImageError("");
    setIsSaving(true);

    const listingId = persistedId;
    const url = listingId
      ? `/api/artworklistings/${listingId}`
      : "/api/artworklistings";

    try {
      const payload = {
        ...form,
        price: Number(form.price),
      };

      const response = await fetch(url, {
        method: listingId ? "PUT" : "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        if (response.status === 401) {
          setUser(null);
          setError("Your session has expired. Please log in again.");
          return;
        }

        if (response.status === 403) {
          setError("You can only update your own listings.");
          return;
        }

        if (response.status === 400) {
          const problem = await response.json().catch(() => null);
          const messages = problem?.errors
            ? Object.values(problem.errors).flat().join(" ")
            : "Please check the listing details and try again.";
          setError(messages);
          return;
        }

        setError(
          response.status === 404
            ? "Listing not found. It may have been deleted."
            : "Failed to save the listing. Please try again."
        );
        return;
      }

      const saved = await response.json();
      setPersistedId(saved.id);

      if (selectedImage) {
        setIsUploading(true);
        const imageBody = new FormData();
        imageBody.append("image", selectedImage);
        const imageResponse = await fetch(`/api/artworklistings/${saved.id}/image`, {
          method: "POST",
          body: imageBody,
        });

        if (!imageResponse.ok) {
          const problem = await imageResponse.json().catch(() => null);
          const message = problem?.message || "The listing was saved, but the image was not uploaded.";
          if (!id) {
            navigate(`/listings/${saved.id}/edit`, {
              replace: true,
              state: { imageError: message, fromSearch },
            });
          } else {
            setImageError(message);
          }
          return;
        }
      } else if (removeCurrentImage && currentImageUrl) {
        const imageResponse = await fetch(`/api/artworklistings/${saved.id}/image`, {
          method: "DELETE",
        });
        if (!imageResponse.ok && imageResponse.status !== 204) {
          setImageError("The listing was saved, but the image was not removed.");
          return;
        }
      }

      navigate(`/listings/${saved.id}`, {
        state: {
          message: listingId
            ? "Listing updated."
            : "Listing created.",
          fromSearch,
        },
      });
    } catch {
      setError("Failed to connect to the server. Please try again.");
    } finally {
      setIsSaving(false);
      setIsUploading(false);
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

  if (authLoading) {
    return <p className="py-4" role="status">Checking your session...</p>;
  }

  if (!user) {
    return (
      <div className="py-4">
        <p>You must be logged in to create or edit a listing.</p>
        <Link to="/login" className="btn btn-primary">Log in</Link>
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

  if (isEditing && (ownerId == null || String(ownerId) !== String(user.id))) {
    return (
      <div className="py-4">
        <p>You can only edit your own listings.</p>
        <Link to={`/listings/${id}`}>Back to listing</Link>
      </div>
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

      {imageError && (
        <div className="alert alert-danger" role="alert">
          {imageError}
        </div>
      )}

      <form onSubmit={handleSubmit} noValidate>
        <fieldset disabled={isSaving || isDeleting || isUploading}>
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

          <div className="mb-3">
            <label htmlFor="image" className="form-label">
              Image
            </label>
            <input
              id="image"
              name="image"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="form-control"
              onChange={handleImageChange}
            />
            <div className="form-text">Optional. JPEG, PNG, or WebP, up to 5 MB.</div>
            {previewUrl && (
              <img className="listing-image-preview" src={previewUrl} alt="Selected artwork" />
            )}
            {!previewUrl && currentImageUrl && !removeCurrentImage && (
              <img className="listing-image-preview" src={currentImageUrl} alt="Current artwork" />
            )}
            {currentImageUrl && !selectedImage && (
              <div className="form-check mt-2">
                <input
                  id="removeCurrentImage"
                  type="checkbox"
                  className="form-check-input"
                  checked={removeCurrentImage}
                  onChange={(event) => setRemoveCurrentImage(event.target.checked)}
                />
                <label htmlFor="removeCurrentImage" className="form-check-label">
                  Remove current image
                </label>
              </div>
            )}
          </div>

          <button type="submit" className="btn btn-primary">
            {isUploading
              ? "Uploading image..."
              : isSaving
                ? "Saving..."
                : persistedId
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
