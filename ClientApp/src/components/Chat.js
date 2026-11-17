import React, { useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { postJson } from "../api";
import { useApiData } from "../useApiData";
import "./Chat.css";

function ChatAccess({ children }) {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return <p role="status">Checking your session...</p>;
  }

  if (!user) {
    return (
      <main className="chat-page">
        <h1>Messages</h1>
        <p>Log in to view your conversations.</p>
        <Link to="/login">Log in</Link>
      </main>
    );
  }

  return children(user);
}

export function Conversations() {
  return (
    <ChatAccess>
      {(user) => <ConversationList key={user.id} />}
    </ChatAccess>
  );
}

function ConversationList() {
  const { data, error, reload } = useApiData("/api/conversations", 5000);

  return (
    <main className="chat-page">
      <h1>Messages</h1>

      <button
        type="button"
        className="btn btn-outline-secondary mb-3"
        onClick={reload}
      >
        Refresh
      </button>

      {error && <p role="alert">{error}</p>}
      {!data && !error && <p role="status">Loading conversations...</p>}

      {data?.length === 0 && (
        <p>
          No conversations yet. Open a creator's profile and select
          “Send message”.
        </p>
      )}

      <ul className="chat-list">
        {data?.map((conversation) => (
          <li key={conversation.id}>
            <Link to={`/messages/${conversation.id}`}>
              <strong>{conversation.otherUserDisplayName}</strong>
              {conversation.artworkTitle && (
                <span>{conversation.artworkTitle}</span>
              )}
              <span>
                {conversation.lastMessage?.content || "No messages yet."}
              </span>
              <small>
                {new Date(conversation.lastMessageAtUtc).toLocaleString()}
              </small>
            </Link>
          </li>
        ))}
      </ul>
    </main>
  );
}

export function Conversation() {
  const { id } = useParams();

  return (
    <ChatAccess>
      {(user) => (
        <ConversationContent
          key={`${user.id}-${id}`}
          id={id}
          user={user}
        />
      )}
    </ChatAccess>
  );
}

function ConversationContent({ id, user }) {
  const { data, error, reload } = useApiData(
    `/api/conversations/${id}`,
    5000
  );
  const [content, setContent] = useState("");
  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState("");
  const sendingRef = useRef(false);

  async function send(event) {
    event.preventDefault();
    const message = content.trim();

    if (!message || sendingRef.current) return;

    sendingRef.current = true;
    setSending(true);
    setSendError("");

    try {
      await postJson(`/api/conversations/${id}/messages`, {
        content: message,
      });

      setContent("");
      reload();
    } catch (error) {
      setSendError(error.message);
    } finally {
      sendingRef.current = false;
      setSending(false);
    }
  }

  return (
    <main className="chat-page">
      <Link to="/messages">← All conversations</Link>

      {error && (
        <div role="alert">
          <p>{error}</p>
          <button type="button" onClick={reload}>Try again</button>
        </div>
      )}

      {!data && !error && <p role="status">Loading conversation...</p>}

      {data && (
        <>
          <h1>
            <Link to={`/users/${data.otherUserId}`}>
              {data.otherUserDisplayName}
            </Link>
          </h1>

          {data.artworkListingId && (
            <p>
              <Link to={`/listings/${data.artworkListingId}`}>
                {data.artworkTitle || "View artwork"}
              </Link>
            </p>
          )}

          <ol className="chat-messages" aria-label="Messages">
            {data.messages.map((message) => (
              <li
                key={message.id}
                className={
                  String(message.senderId) === String(user.id)
                    ? "chat-message chat-message-own"
                    : "chat-message"
                }
              >
                <strong>{message.senderDisplayName}</strong>
                <p>{message.content}</p>
                <time dateTime={message.sentAtUtc}>
                  {new Date(message.sentAtUtc).toLocaleString()}
                </time>
              </li>
            ))}
          </ol>

          {data.messages.length === 0 && <p>No messages yet.</p>}

          <form onSubmit={send}>
            <label htmlFor="chat-message">Your message</label>
            <textarea
              id="chat-message"
              className="form-control my-2"
              rows={3}
              maxLength={2000}
              required
              value={content}
              disabled={sending}
              onChange={(event) => setContent(event.target.value)}
            />
            <p>{content.length}/2000</p>

            {sendError && <p role="alert">{sendError}</p>}

            <button
              className="btn btn-primary"
              type="submit"
              disabled={sending || !content.trim()}
            >
              {sending ? "Sending..." : "Send"}
            </button>
          </form>
        </>
      )}
    </main>
  );
}