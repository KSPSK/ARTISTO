import React, { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { postJson } from "../api";

export default function StartChatButton({
  participantId,
  artworkListingId,
}) {
  const { user, isLoading } = useAuth();
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const busyRef = useRef(false);

  if (!participantId || String(user?.id) === String(participantId)) {
    return null;
  }

  async function start() {
    if (!user) {
      navigate("/login");
      return;
    }

    if (busyRef.current) return;
    busyRef.current = true;
    setBusy(true);
    setError("");

    try {
      const conversation = await postJson("/api/conversations", {
        participantId: Number(participantId),
        artworkListingId: artworkListingId
          ? Number(artworkListingId)
          : null,
      });

      navigate(`/messages/${conversation.id}`);
    } catch (error) {
      setError(error.message);
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  }

  return (
    <div className="my-3">
      <button
        type="button"
        className="btn btn-outline-primary"
        disabled={isLoading || busy}
        onClick={start}
      >
        {busy ? "Opening..." : "Send message"}
      </button>
      {error && <p role="alert">{error}</p>}
    </div>
  );
}