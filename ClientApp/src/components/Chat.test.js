import React from "react";
import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { Conversation, Conversations } from "./Chat";
import { useAuth } from "../AuthContext";
import { jsonResponse } from "../testUtils";

jest.mock("../AuthContext", () => ({ useAuth: jest.fn() }));

const currentUser = { id: 1, displayName: "Me" };

const conversationSummary = {
  id: 7,
  otherUserId: 2,
  otherUserDisplayName: "Ona",
  artworkListingId: 3,
  artworkTitle: "Sunset over Vilnius",
  lastMessage: { id: 11, content: "Is it still available?" },
  lastMessageAtUtc: "2026-11-20T10:00:00Z",
};

const conversationDetails = {
  id: 7,
  otherUserId: 2,
  otherUserDisplayName: "Ona",
  artworkListingId: 3,
  artworkTitle: "Sunset over Vilnius",
  messages: [
    {
      id: 10,
      senderId: 2,
      senderDisplayName: "Ona",
      content: "Hello!",
      sentAtUtc: "2026-11-20T09:00:00Z",
    },
    {
      id: 11,
      senderId: 1,
      senderDisplayName: "Me",
      content: "Is it still available?",
      sentAtUtc: "2026-11-20T10:00:00Z",
    },
  ],
};

function signIn(user = currentUser) {
  useAuth.mockReturnValue({ user, isLoading: false });
}

function renderConversations() {
  return render(
    <MemoryRouter initialEntries={["/messages"]}>
      <Conversations />
    </MemoryRouter>
  );
}

function renderConversation() {
  return render(
    <MemoryRouter initialEntries={["/messages/7"]}>
      <Routes>
        <Route path="/messages/:id" element={<Conversation />} />
      </Routes>
    </MemoryRouter>
  );
}

// Answers GET requests with the conversation and POST requests with `post`.
function mockConversationApi(post = () => jsonResponse({ id: 12 }, 201)) {
  global.fetch = jest.fn((_url, options) =>
    options?.method === "POST" ? post() : jsonResponse(conversationDetails)
  );
}

function postRequests() {
  return global.fetch.mock.calls.filter(
    ([, options]) => options?.method === "POST"
  );
}

beforeEach(() => {
  signIn();
  global.fetch = jest.fn(() => jsonResponse([]));
});

describe("chat access", () => {
  it("waits while the session is checked", () => {
    useAuth.mockReturnValue({ user: null, isLoading: true });

    renderConversations();

    expect(screen.getByRole("status")).toHaveTextContent(
      "Checking your session..."
    );
    expect(global.fetch).not.toHaveBeenCalled();
  });

  it("asks a logged-out visitor to log in without loading conversations", () => {
    useAuth.mockReturnValue({ user: null, isLoading: false });

    renderConversations();

    expect(
      screen.getByText("Log in to view your conversations.")
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Log in" })).toHaveAttribute(
      "href",
      "/login"
    );
    expect(global.fetch).not.toHaveBeenCalled();
  });
});

describe("conversation list", () => {
  it("lists conversations with the other person, artwork and last message", async () => {
    global.fetch = jest.fn(() =>
      jsonResponse([
        conversationSummary,
        {
          ...conversationSummary,
          id: 8,
          otherUserDisplayName: "Jonas",
          artworkTitle: null,
          lastMessage: null,
        },
      ])
    );

    renderConversations();

    const first = await screen.findByRole("link", { name: /Ona/ });
    expect(first).toHaveAttribute("href", "/messages/7");
    expect(within(first).getByText("Sunset over Vilnius")).toBeInTheDocument();
    expect(
      within(first).getByText("Is it still available?")
    ).toBeInTheDocument();

    const second = screen.getByRole("link", { name: /Jonas/ });
    expect(second).toHaveAttribute("href", "/messages/8");
    expect(within(second).getByText("No messages yet.")).toBeInTheDocument();
    expect(global.fetch).toHaveBeenCalledWith(
      "/api/conversations",
      expect.anything()
    );
  });

  it("explains how to start a conversation when there are none", async () => {
    renderConversations();

    expect(await screen.findByText(/No conversations yet/)).toBeInTheDocument();
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("shows the server's error message when loading fails", async () => {
    global.fetch = jest.fn(() =>
      jsonResponse({ message: "Conversations are unavailable." }, 500)
    );

    renderConversations();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Conversations are unavailable."
    );
  });

  it("loads the conversations again when Refresh is clicked", async () => {
    const user = userEvent.setup();
    renderConversations();
    await screen.findByText(/No conversations yet/);
    global.fetch.mockImplementation(() => jsonResponse([conversationSummary]));

    await user.click(screen.getByRole("button", { name: "Refresh" }));

    expect(
      await screen.findByRole("link", { name: /Ona/ })
    ).toBeInTheDocument();
  });

  it("checks for new conversations every 5 seconds", async () => {
    jest.useFakeTimers();

    try {
      renderConversations();
      await screen.findByText(/No conversations yet/);
      expect(global.fetch).toHaveBeenCalledTimes(1);
      global.fetch.mockImplementation(() =>
        jsonResponse([conversationSummary])
      );

      act(() => {
        jest.advanceTimersByTime(5000);
      });

      expect(
        await screen.findByRole("link", { name: /Ona/ })
      ).toBeInTheDocument();
      expect(global.fetch).toHaveBeenCalledTimes(2);
    } finally {
      jest.useRealTimers();
    }
  });
});

describe("conversation page", () => {
  it("shows the messages and marks the ones sent by the current user", async () => {
    mockConversationApi();

    renderConversation();

    expect(await screen.findByRole("link", { name: "Ona" })).toHaveAttribute(
      "href",
      "/users/2"
    );
    expect(
      screen.getByRole("link", { name: "Sunset over Vilnius" })
    ).toHaveAttribute("href", "/listings/3");

    const messages = within(screen.getByRole("list", { name: "Messages" }));
    const [theirs, mine] = messages.getAllByRole("listitem");
    expect(theirs).toHaveTextContent("Hello!");
    expect(theirs).not.toHaveClass("chat-message-own");
    expect(mine).toHaveTextContent("Is it still available?");
    expect(mine).toHaveClass("chat-message-own");
    expect(global.fetch).toHaveBeenCalledWith(
      "/api/conversations/7",
      expect.anything()
    );
  });

  it("says so when the conversation has no messages", async () => {
    global.fetch = jest.fn(() =>
      jsonResponse({ ...conversationDetails, messages: [] })
    );

    renderConversation();

    expect(await screen.findByText("No messages yet.")).toBeInTheDocument();
  });

  it("keeps Send disabled until the message has text", async () => {
    const user = userEvent.setup();
    mockConversationApi();
    renderConversation();
    const input = await screen.findByLabelText("Your message");
    const send = screen.getByRole("button", { name: "Send" });

    expect(send).toBeDisabled();

    await user.type(input, "   ");
    expect(send).toBeDisabled();

    await user.type(input, "Hi");
    expect(send).toBeEnabled();
    expect(screen.getByText("5/2000")).toBeInTheDocument();
  });

  it("sends the trimmed message, clears the field and reloads the conversation", async () => {
    const user = userEvent.setup();
    mockConversationApi();
    renderConversation();
    const input = await screen.findByLabelText("Your message");
    const loadsBeforeSending = global.fetch.mock.calls.length;

    await user.type(input, "  Can I pick it up on Friday?  ");
    await user.click(screen.getByRole("button", { name: "Send" }));

    await waitFor(() => expect(input).toHaveValue(""));
    const [[url, options]] = postRequests();
    expect(url).toBe("/api/conversations/7/messages");
    expect(JSON.parse(options.body)).toEqual({
      content: "Can I pick it up on Friday?",
    });
    await waitFor(() =>
      expect(global.fetch.mock.calls.length).toBe(loadsBeforeSending + 2)
    );
    expect(global.fetch).toHaveBeenLastCalledWith(
      "/api/conversations/7",
      expect.anything()
    );
  });

  it("shows why sending failed and keeps the typed message", async () => {
    const user = userEvent.setup();
    mockConversationApi(() =>
      jsonResponse({ message: "Message content cannot be empty." }, 400)
    );
    renderConversation();
    const input = await screen.findByLabelText("Your message");

    await user.type(input, "Hello");
    await user.click(screen.getByRole("button", { name: "Send" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Message content cannot be empty."
    );
    expect(input).toHaveValue("Hello");
    expect(screen.getByRole("button", { name: "Send" })).toBeEnabled();
  });

  it("tells a user who is not a participant that access is denied", async () => {
    global.fetch = jest.fn(() => jsonResponse(null, 403));

    renderConversation();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "You do not have permission to do this."
    );
    expect(screen.queryByLabelText("Your message")).not.toBeInTheDocument();
  });

  it("loads the conversation again after Try again", async () => {
    const user = userEvent.setup();
    global.fetch = jest
      .fn()
      .mockImplementationOnce(() => jsonResponse(null, 500))
      .mockImplementation(() => jsonResponse(conversationDetails));
    renderConversation();

    await user.click(await screen.findByRole("button", { name: "Try again" }));

    expect(await screen.findByText("Hello!")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
