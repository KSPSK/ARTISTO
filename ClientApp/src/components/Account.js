import React from "react";
import { Link, useNavigate } from "react-router-dom";

function currentUser() {
    try {
        return JSON.parse(sessionStorage.getItem("user") || "null");
    } catch {
        return null;
    }
}

export default function Account() {
    const navigate = useNavigate();
    const user = currentUser();

    function handleLogout() {
        sessionStorage.removeItem("user");
        navigate("/");
    }

    if (!user) {
        return (
            <main className="mx-auto py-5" style={{ maxWidth: 480 }}>
                <h1 className="mb-2">Account</h1>
                <p className="text-muted mb-4">
                    Log in to view and edit your creator profile.
                </p>
                <div className="d-grid gap-2 d-sm-flex">
                    <Link className="btn btn-primary" to="/login">Log in</Link>
                    <Link className="btn btn-outline-primary" to="/register">Register</Link>
                </div>
            </main>
        );
    }

    return (
        <main className="mx-auto py-5" style={{ maxWidth: 480 }}>
            <h1 className="mb-2">Account</h1>
            <p className="mb-1">{user.displayName}</p>
            <p className="text-muted mb-4">@{user.username}</p>
            <div className="d-grid gap-2">
                <Link className="btn btn-primary" to={`/users/${user.id}`}>View profile</Link>
                <Link className="btn btn-outline-primary" to={`/users/${user.id}/edit`}>Edit profile</Link>
                <button type="button" className="btn btn-outline-secondary" onClick={handleLogout}>
                    Log out
                </button>
            </div>
        </main>
    );
}
