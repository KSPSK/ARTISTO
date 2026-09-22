import React, { Component } from 'react';

export class ArtworkListings extends Component {
  static displayName = ArtworkListings.name;

  constructor(props) {
    super(props);
    this.state = {
      listings: [],
      status: 'loading', 
      errorMessage: ''
    };
  }

  componentDidMount() {
    this.loadListings();
  }

  loadListings = () => {
    this.setState({ status: 'loading', errorMessage: '' });

    fetch('/api/artworklistings')
      .then(response => {
        if (!response.ok) {
          throw new Error(`Server responded with an error (${response.status})`);
        }
        return response.json();
      })
      .then(data => {
        this.setState({ listings: data, status: 'success' });
      })
      .catch(error => {
        this.setState({ status: 'error', errorMessage: error.message });
      });
  };

  renderContent() {
    const { status, listings, errorMessage } = this.state;

    if (status === 'loading') {
      return <p>Loading listings...</p>;
    }

    if (status === 'error') {
      return (
        <div className="artwork-status artwork-status-error">
          <p>Failed to load listings. {errorMessage}.</p>
          <button onClick={this.loadListings}>Retry</button>
        </div>
      );
    }

    if (listings.length === 0) {
      return <p className="artwork-status">No listings yet.</p>;
    }

    return (
      <div className="artwork-grid">
        {listings.map(listing => (
          <div className="artwork-card" key={listing.id}>
            <h3 className="artwork-title">{listing.title}</h3>
            <p className="artwork-creator">{listing.creatorName}</p>
            <p className="artwork-price">{listing.price} €</p>
            <p className="artwork-category">{listing.category}</p>
          </div>
        ))}
      </div>
    );
  }

  render() {
    return (
      <div>
        <h1>Artwork Listings</h1>
        {this.renderContent()}
      </div>
    );
  }
}