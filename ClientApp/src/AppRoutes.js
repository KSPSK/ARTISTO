import { Home } from "./components/Home";
import { ArtworkListings } from "./components/ArtworkListings";
import ArtworkListingForm from "./components/ArtworkListingForm";

const AppRoutes = [
  {
    index: true,
    element: <Home />
  },
  {
    path: '/artworklistings',
    element: <ArtworkListings />
  },
  {
    path: "/listings/new",
    element: <ArtworkListingForm />
  },
  {
    path: "/listings/:id/edit",
    element: <ArtworkListingForm />
  }
];

export default AppRoutes;
