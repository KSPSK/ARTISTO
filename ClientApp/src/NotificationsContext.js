import React, { createContext, useContext } from "react";
import { useAuth } from "./AuthContext";
import { useApiData } from "./useApiData";

const NotificationsContext = createContext(null);

export function NotificationsProvider({ children }) {
  const { user, isLoading } = useAuth();
  const userId = isLoading ? null : user?.id ?? null;

  return (
    <NotificationSession key={userId ?? "guest"} userId={userId}>
      {children}
    </NotificationSession>
  );
}

function NotificationSession({ userId, children }) {
  const { data, reload } = useApiData(
    userId === null ? null : "/api/notifications/unread-count",
    15000
  );

  return (
    <NotificationsContext.Provider
      value={{
        unreadCount: data?.unreadCount ?? 0,
        refreshUnreadCount: reload,
      }}
    >
      {children}
    </NotificationsContext.Provider>
  );
}

export function useNotifications() {
  return useContext(NotificationsContext);
}