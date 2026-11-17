import React from "react";
import { Link } from "react-router-dom";
import { useApiData } from "../useApiData";

export default function CreatorReviews({ sellerId }) {
  const { data, error, reload } = useApiData(
    `/api/users/${sellerId}/reviews`
  );

  return (
    <section className="my-4" aria-label="Reviews and reputation">
      <h2>Reviews and reputation</h2>

      {error && (
        <div role="alert">
          <p>{error}</p>
          <button type="button" onClick={reload}>Try again</button>
        </div>
      )}

      {!data && !error && <p role="status">Loading reviews...</p>}

      {data && (
        <>
          <div className="d-flex flex-wrap gap-4 mb-3">
            <div>
              <strong>
                {data.averageRating == null
                  ? "No ratings yet"
                  : `${Number(data.averageRating).toFixed(1)} / 5`}
              </strong>
              <div>Average rating</div>
            </div>

            <div>
              <strong>{data.reviewCount}</strong>
              <div>Reviews</div>
            </div>

            <div>
              <strong>{data.completedSalesCount}</strong>
              <div>Completed sales</div>
            </div>
          </div>

          {data.reviews.length === 0 ? (
            <p>No reviews yet.</p>
          ) : (
            <ul className="list-unstyled">
              {data.reviews.map((review) => (
                <li key={review.id} className="border rounded p-3 mb-3">
                  <div>
                    <Link to={`/users/${review.reviewerId}`}>
                      {review.reviewerDisplayName || "Buyer"}
                    </Link>
                    {" · "}
                    <strong>{review.rating} / 5</strong>
                  </div>

                  <small>
                    {new Date(review.createdAtUtc).toLocaleDateString()}
                  </small>

                  {review.comment && (
                    <p style={{
                      whiteSpace: "pre-wrap",
                      overflowWrap: "anywhere",
                    }}>
                      {review.comment}
                    </p>
                  )}

                  <Link to={`/listings/${review.artworkListingId}`}>
                    {review.artworkTitle || "View artwork"}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  );
}