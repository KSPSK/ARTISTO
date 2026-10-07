import React, { Component } from 'react';
import { Collapse, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import './NavMenu.css';

export class NavMenu extends Component {
  static displayName = NavMenu.name;

  constructor (props) {
    super(props);

    this.toggleNavbar = this.toggleNavbar.bind(this);
    this.state = {
      collapsed: true
    };
  }

  toggleNavbar () {
    this.setState({
      collapsed: !this.state.collapsed
    });
  }

  render() {
    let user = null;
    try {
      user = JSON.parse(sessionStorage.getItem('user') || 'null');
    } catch {
      user = null;
    }

    return (
      <header>
        <Navbar className="navbar-expand-sm navbar-toggleable-sm ng-white border-bottom box-shadow mb-3" container light>
          <NavbarBrand tag={Link} to="/">ARTISTO</NavbarBrand>
          <NavbarToggler onClick={this.toggleNavbar} className="mr-2" />
          <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!this.state.collapsed} navbar>
            <ul className="navbar-nav flex-grow">
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/">Home</NavLink>
              </NavItem>
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/artworklistings">Explore art</NavLink>
              </NavItem>
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/listings/new">Sell your art</NavLink>
              </NavItem>
              {user ? (
                <>
                  <NavItem>
                    <NavLink tag={Link} className="text-dark" to={`/users/${user.id}`}>My profile</NavLink>
                  </NavItem>
                  <NavItem>
                    <button
                      type="button"
                      className="nav-link btn btn-link text-dark"
                      onClick={() => {
                        sessionStorage.removeItem('user');
                        this.forceUpdate();
                        window.location.assign('/');
                      }}
                    >
                      Log out
                    </button>
                  </NavItem>
                </>
              ) : (
                <>
                  <NavItem>
                    <NavLink tag={Link} className="text-dark" to="/login">Log in</NavLink>
                  </NavItem>
                  <NavItem>
                    <NavLink tag={Link} className="text-dark" to="/register">Register</NavLink>
                  </NavItem>
                </>
              )}
            </ul>
          </Collapse>
        </Navbar>
      </header>
    );
  }
}
