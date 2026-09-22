import { Home } from "./components/Home";
import ArtworkListingForm from "./components/ArtworkListingForm";

const AppRoutes = [
  {
    index: true,
    element: <Home />
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
