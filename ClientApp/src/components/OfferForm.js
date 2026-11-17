import React, { useId, useRef, useState } from "react";
import { Link } from "react-router-dom";

const minAmount = 0.01;
const maxAmount = 1_000_000;

export default function OfferForm({ listingId }) {
  const formId = useId();
  const trigger = useRef(null);
  const [isOpen, setIsOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState("");

  function closeForm() {
    setIsOpen(false);
    setError("");
    trigger.current?.focus();
  }

  async function submitOffer(event) {
    event.preventDefault();
    if (isSubmitting) return;

    const fields = new FormData(event.currentTarget);
    const value = Number(fields.get("amount"));
    if (value < minAmount || value > maxAmount || Math.round(value * 100) / 100 !== value) {
      setError("Enter an amount from €0.01 to €1,000,000 with at most two decimal places.");
      return;
    }

    setIsSubmitting(true);
    setError("");

    try {
      const response = await fetch("/api/offers", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          artworkListingId: listingId,
          amount: value,
          message: fields.get("message").trim() || null,
        }),
      });

      if (response.status === 401) {
        setError(<>Your session has expired. <Link to="/login">Log in</Link> to make an offer.</>);
        return;
      }

      if (!response.ok) {
        const problem = await response.json().catch(() => null);
        const validation = problem?.errors && Object.values(problem.errors).flat().join(" ");
        setError(problem?.message || validation || "Unable to send your offer. Please try again.");
        return;
      }

      setSubmitted(true);
      setIsOpen(false);
    } catch {
      setError("Unable to connect to the server. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mt-3">
      <div role="status">
        {submitted && <p className="alert alert-success mb-0">Offer submitted.</p>}
      </div>
      {!submitted && (
        <button
          ref={trigger}
          type="button"
          className="btn btn-secondary"
          aria-expanded={isOpen}
          aria-controls={isOpen ? formId : undefined}
          disabled={isSubmitting}
          onClick={() => (isOpen ? closeForm() : setIsOpen(true))}
        >
          Make an offer
        </button>
      )}

      {isOpen && (
        <form id={formId} className="mt-3 p-3 border rounded" onSubmit={submitOffer} noValidate>
          {error && <p className="alert alert-danger" role="alert">{error}</p>}

          <fieldset disabled={isSubmitting}>
            <label htmlFor={`${formId}-amount`} className="form-label">
              Offer amount (€)
            </label>
            <input
              id={`${formId}-amount`}
              name="amount"
              className="form-control mb-3"
              type="number"
              inputMode="decimal"
              required
              min={minAmount}
              max={maxAmount}
              step="0.01"
            />
            <label htmlFor={`${formId}-message`} className="form-label">
              Message (optional)
            </label>
            <textarea
              id={`${formId}-message`}
              name="message"
              className="form-control mb-3"
              rows={2}
              maxLength={1000}
            />
            <div className="d-flex flex-wrap gap-2">
              <button type="submit" className="btn btn-primary">
                {isSubmitting ? "Sending offer..." : "Send offer"}
              </button>
              <button type="button" className="btn btn-secondary" onClick={closeForm}>
                Cancel
              </button>
            </div>
          </fieldset>
        </form>
      )}
    </div>
  );
}
