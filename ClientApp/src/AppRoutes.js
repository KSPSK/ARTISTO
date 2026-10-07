import { Home } from "./components/Home";
import { ArtworkListings } from "./components/ArtworkListings";
import ArtworkListingForm from "./components/ArtworkListingForm";
import ArtworkListingDetails from "./components/ArtworkListingDetails";
import Login from "./components/Login";
import Register from "./components/Register";
import Account from "./components/Account";
import CreatorProfile from "./components/CreatorProfile";
import CreatorProfileForm from "./components/CreatorProfileForm";

const AppRoutes = [
    {
        index: true,
        element: <Home />
    },
    {
        path: "/artworklistings",
        element: <ArtworkListings />
    },
    {
        path: "/listings/new",
        element: <ArtworkListingForm />
    },
    {
        path: "/listings/:id/edit",
        element: <ArtworkListingForm />
    },
    {
        path: "/listings/:id",
        element: <ArtworkListingDetails />
    },
    {
        path: "/account",
        element: <Account />
    },
    {
        path: "/login",
        element: <Login />
    },
    {
        path: "/register",
        element: <Register />
    },
    {
        path: "/users/:id/edit",
        element: <CreatorProfileForm />
    },
    {
        path: "/users/:id",
        element: <CreatorProfile />
    }
];

export default AppRoutes;