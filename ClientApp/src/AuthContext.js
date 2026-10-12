import React, { createContext, useContext, useEffect, useState } from "react";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
    const [user, setUser] = useState(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const controller = new AbortController();

        async function restoreSession() {
            try {
                const response = await fetch("/api/users/me", {
                    signal: controller.signal,
                });

                if (response.ok) {
                    setUser(await response.json());
                } else if (response.status === 401) {
                    setUser(null);
                }
            } catch {
                if (!controller.signal.aborted) {
                    setUser(null);
                }
            } finally {
                if (!controller.signal.aborted) {
                    setIsLoading(false);
                }
            }
        }

        restoreSession();
        return () => controller.abort();
    }, []);

    async function logout() {
        const response = await fetch("/api/users/logout", { method: "POST" });
        if (!response.ok) {
            throw new Error("Unable to log out.");
        }

        setUser(null);
    }

    return (
        <AuthContext.Provider value={{ user, isLoading, setUser, logout }}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error("useAuth must be used inside an AuthProvider.");
    }

    return context;
}
