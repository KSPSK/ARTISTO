import React, { useState } from "react";

export function ListingImage({ imageUrl, title, large }) {
  const [failed, setFailed] = useState(false);
  const emptyClass = large ? "listing-details-image" : "artwork-thumb artwork-thumb-empty";

  if (!imageUrl || failed) {
    return (
      <div className={emptyClass}>
        <span>No image</span>
      </div>
    );
  }

  return (
    <img
      className={large ? "listing-details-photo" : "artwork-thumb"}
      src={imageUrl}
      alt={title || ""}
      onError={() => setFailed(true)}
    />
  );
}
