import React, { Component } from 'react';

export class Home extends Component {
  static displayName = Home.name;

  render() {
    return (
      <div>
        <h1>ARTISTO</h1>

        <p>
          Discover, buy, and sell original artwork in one accessible
          marketplace.
        </p>

        <h2>For buyers</h2>
        <p>
          Explore artwork from independent creators and find pieces that
          match your preferences.
        </p>

        <h2>For artists</h2>
        <p>
          Showcase your work, create listings, reach potential customers,
          and build a trusted creator profile.
        </p>
      </div>
    );
  }
}