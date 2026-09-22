import { Home } from "./components/Home";
import { ArtworkListings } from "./components/ArtworkListings";

const AppRoutes = [
  {
    index: true,
    element: <Home />
  },
  {
    path: '/artworklistings',
    element: <ArtworkListings />
  }
];

export default AppRoutes;
