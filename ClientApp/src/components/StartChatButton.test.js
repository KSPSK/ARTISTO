import React from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import StartChatButton from "./StartChatButton";
import { useAuth } from "../AuthContext";
import { jsonResponse } from "../testUtils";

jest.mock("../AuthContext", () => ({ useAuth: jest.fn() }));

function renderButton(props) {
  return render(
    <MemoryRouter initialEntries={["/users/2"]}>
      <Routes>
        <Route path="/users/2" element={<StartChatButton {...props} />} />
        <Route path="/login" element={<p>Login page</p>} />
        <Route path="/messages/:id" element={<p>Conversation page</p>} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  useAuth.mockReturnValue({ user: { id: 1 }, isLoading: false });
  global.fetch = jest.fn(() => jsonResponse({ id: 12 }, 201));
});

describe("StartChatButton", () => {
  it("is hidden on the current user's own profile", () => {
    renderButton({ participantId: 1 });

    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("sends a logged-out visitor to the login page", async () => {
    const user = userEvent.setup();
    useAuth.mockReturnValue({ user: null, isLoading: false });
    renderButton({ participantId: 2 });

    await user.click(screen.getByRole("button", { name: "Send message" }));

    expect(screen.getByText("Login page")).toBeInTheDocument();
    expect(global.fetch).not.toHaveBeenCalled();
  });

  it("starts a conversation and opens it", async () => {
    const user = userEvent.setup();
    renderButton({ participantId: "2" });

    await user.click(screen.getByRole("button", { name: "Send message" }));

    expect(await screen.findByText("Conversation page")).toBeInTheDocument();
    const [url, options] = global.fetch.mock.calls[0];
    expect(url).toBe("/api/conversations");
    expect(options.method).toBe("POST");
    expect(JSON.parse(options.body)).toEqual({
      participantId: 2,
      artworkListingId: null,
    });
  });

  it("links the conversation to the artwork when one is given", async () => {
    const user = userEvent.setup();
    renderButton({ participantId: 2, artworkListingId: 3 });

    await user.click(screen.getByRole("button", { name: "Send message" }));

    await screen.findByText("Conversation page");
    expect(JSON.parse(global.fetch.mock.calls[0][1].body)).toEqual({
      participantId: 2,
      artworkListingId: 3,
    });
  });

  it("shows the error and stays on the page when the conversation cannot be started", async () => {
    const user = userEvent.setup();
    global.fetch = jest.fn(() =>
      jsonResponse({ message: "User 2 was not found." }, 404)
    );
    renderButton({ participantId: 2 });

    await user.click(screen.getByRole("button", { name: "Send message" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "User 2 was not found."
    );
    expect(screen.getByRole("button", { name: "Send message" })).toBeEnabled();
  });
});
