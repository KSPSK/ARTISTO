import React, { useRef, useState } from "react";
import { Link } from "react-router-dom";
import { postJson } from "../api";
import { useApiData } from "../useApiData";
import "./Reviews.css";

export default function ReviewForm({ order }) {
  const { data, error, reload } = useApiData(
    `/api/users/${order.sellerId}/reviews`
  );

  const [rating, setRating] = useState("5");
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [submitError, setSubmitError] = useState("");
  const savingRef = useRef(false);

  const existingReview = data?.reviews.find(
    (review) => String(review.orderId) === String(order.id)
  );

  async function submit(event) {
    event.preventDefault();

    if (savingRef.current) return;
    savingRef.current = true;
    setSaving(true);
    setSubmitError("");

    try {
      await postJson("/api/reviews", {
        orderId: order.id,
        rating: Number(rating),
        comment: comment.trim() || null,
      });

      setSaved(true);
    } catch (error) {
      if (error.status === 409) {
        setSaved(true);
      } else {
        setSubmitError(error.message);
      }
    } finally {
      savingRef.current = false;
      setSaving(false);
    }
  }

  if (saved || existingReview) {
    return (
      <section className="reviews-section border rounded p-3 mt-4">
        <h2>Your review</h2>
        <p role="status">You have reviewed this order.</p>
        <Link to={`/users/${order.sellerId}`}>View seller reviews</Link>
      </section>
    );
  }

  if (error) {
    return (
      <section className="reviews-section mt-4">
        <p role="alert">{error}</p>
        <button type="button" className="btn btn-primary" onClick={reload}>
          Try again
        </button>
      </section>
    );
  }

  if (!data) {
    return <p role="status">Checking your review...</p>;
  }

  return (
    <section className="reviews-section border rounded p-3 mt-4">
      <h2>Leave a review</h2>

      <form onSubmit={submit}>
        <label htmlFor="review-rating" className="form-label">
          Rating
        </label>
        <select
          id="review-rating"
          className="form-select mb-3"
          value={rating}
          disabled={saving}
          onChange={(event) => setRating(event.target.value)}
        >
          {[5, 4, 3, 2, 1].map((value) => (
            <option key={value} value={value}>
              {value} / 5
            </option>
          ))}
        </select>

        <label htmlFor="review-comment" className="form-label">
          Comment (optional)
        </label>
        <textarea
          id="review-comment"
          className="form-control mb-2"
          rows={4}
          maxLength={1000}
          value={comment}
          disabled={saving}
          onChange={(event) => setComment(event.target.value)}
        />
        <p>{comment.length}/1000</p>

        {submitError && <p role="alert">{submitError}</p>}

        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? "Submitting..." : "Submit review"}
        </button>
      </form>
    </section>
  );
}
