import React, { useEffect, useRef, useState } from "react";
import { Link, NavLink as RouterNavLink, useNavigate } from "react-router-dom";
import { NavItem, NavLink } from "reactstrap";
import { api } from "../api";
import { useAuth } from "../AuthContext";
import { useNotifications } from "../NotificationsContext";
import { useApiData } from "../useApiData";
import "./Notifications.css";

const PAGE_SIZE = 20;

export function NotificationsNavItem() {
  const notifications = useNotifications();

  if (!notifications) {
    return null;
  }

  return <SignedInNotificationsNavItem count={notifications.unreadCount} />;
}

function SignedInNotificationsNavItem({ count }) {
  const { user, isLoading } = useAuth();

  if (isLoading || !user) {
    return null;
  }

  return (
    <NavItem>
      <NavLink
        tag={RouterNavLink}
        to="/notifications"
        className="notifications-nav-link"
        aria-label={count > 0 ? `Notifications, ${count} unread` : "Notifications"}
      >
        <span>Notifications</span>
        {count > 0 && (
          <span className="notifications-nav-count" aria-hidden="true">
            {count > 99 ? "99+" : count}
          </span>
        )}
      </NavLink>
    </NavItem>
  );
}

export default function Notifications() {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return <p role="status">Checking your session...</p>;
  }

  if (!user) {
    return (
      <section className="notifications-page" aria-labelledby="notifications-title">
        <div className="notifications-header">
          <h1 id="notifications-title">Notifications</h1>
        </div>
        <p>Log in to see your notifications.</p>
        <Link to="/login" className="notifications-button">
          Log in
        </Link>
      </section>
    );
  }

  return <NotificationList key={user.id} />;
}

function NotificationList() {
  const navigate = useNavigate();
  const { refreshUnreadCount } = useNotifications();
  const [page, setPage] = useState(1);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState("");
  const pendingRequest = useRef(null);

  const { data, error, reload } = useApiData(
    `/api/notifications?page=${page}&pageSize=${PAGE_SIZE}`
  );

  useEffect(() => {
    return () => pendingRequest.current?.abort();
  }, []);

  async function markAsRead(path, destination = null) {
    if (pendingRequest.current) {
      return;
    }

    const controller = new AbortController();
    pendingRequest.current = controller;
    setSaving(true);
    setSaveError("");

    try {
      await api(path, {
        method: "PATCH",
        signal: controller.signal,
      });

      if (controller.signal.aborted) {
        return;
      }

      reload();
      refreshUnreadCount();

      if (destination) {
        navigate(destination);
      }
    } catch (requestError) {
      if (!controller.signal.aborted) {
        setSaveError(requestError.message);
      }
    } finally {
      pendingRequest.current = null;

      if (!controller.signal.aborted) {
        setSaving(false);
      }
    }
  }

  return (
    <section className="notifications-page" aria-labelledby="notifications-title">
      <div className="notifications-header">
        <h1 id="notifications-title">Notifications</h1>

        <button
          type="button"
          className="notifications-button notifications-button-outline"
          disabled={saving || !data?.totalCount}
          onClick={() => markAsRead("/api/notifications/read-all")}
        >
          Mark all as read
        </button>
      </div>

      {saveError && (
        <p className="alert alert-danger" role="alert">
          {saveError}
        </p>
      )}

      {error && (
        <div className="alert alert-danger" role="alert">
          <p>{error}</p>
          <button
            type="button"
            className="notifications-button notifications-button-outline"
            onClick={reload}
          >
            Try again
          </button>
        </div>
      )}

      {!data && !error && <p role="status">Loading notifications...</p>}

      {data?.items.length === 0 && (
        <p>
          {page === 1 ? "No notifications yet." : "No notifications on this page."}
        </p>
      )}

      {data?.items.length > 0 && (
        <ul className="notifications-list">
          {data.items.map((notification) => {
            const destination = notificationDestination(notification.link);
            const unread = notification.readAtUtc === null;

            return (
              <li key={notification.id} className="notifications-row">
                {unread && <span className="notifications-unread">Unread</span>}

                <p className="notifications-message">{notification.message}</p>

                <time
                  className="notifications-time"
                  dateTime={notification.createdAtUtc}
                >
                  {new Date(notification.createdAtUtc).toLocaleString()}
                </time>

                {unread ? (
                  <div className="notifications-action">
                    <button
                      type="button"
                      className="notifications-button"
                      disabled={saving}
                      onClick={() =>
                        markAsRead(
                          `/api/notifications/${notification.id}/read`,
                          destination
                        )
                      }
                    >
                      {destination ? "Open" : "Mark as read"}
                    </button>
                  </div>
                ) : (
                  destination && (
                    <div className="notifications-action">
                      <Link to={destination} className="notifications-button">
                        Open
                      </Link>
                    </div>
                  )
                )}
              </li>
            );
          })}
        </ul>
      )}

      {(page > 1 || (data && data.totalCount > page * PAGE_SIZE)) && (
        <nav
          className="d-flex align-items-center gap-2"
          aria-label="Notification pages"
        >
          <button
            type="button"
            className="notifications-button notifications-button-outline"
            disabled={page === 1 || saving}
            onClick={() => setPage(page - 1)}
          >
            Previous
          </button>

          <span>Page {page}</span>

          <button
            type="button"
            className="notifications-button notifications-button-outline"
            disabled={!data || data.totalCount <= page * PAGE_SIZE || saving}
            onClick={() => setPage(page + 1)}
          >
            Next
          </button>
        </nav>
      )}
    </section>
  );
}

function notificationDestination(link) {
  if (
    link === "/offers" ||
    (typeof link === "string" && /^\/orders\/\d+$/.test(link))
  ) {
    return link;
  }

  return null;
}