# BrainStormEra Project Directory Structure

## Overview

The BrainStormEra project is organized according to the MVC (Model-View-Controller) pattern with a clearly divided directory structure for easy management and maintenance.

## wwwroot Directory Structure

### CSS

- **components**: Contains CSS files for reusable UI components
  - `header.css`: Styles for header
  - `loader.css`: Styles for loading animation
  - `placeholders.css`: Styles for placeholders
- **layouts**: Contains CSS files for common layouts
  - `base.css`: Basic styles for the entire website
  - `site.css`: Styles for main layout
- **pages**: Contains CSS for specific pages, organized by controller
  - **Home**: CSS for home page
    - `enhanced-home.css`: Enhanced styles for home page
    - `homePage.css`: Basic styles for home page
    - `landing_page.css`: Styles for landing page
  - **Course**: CSS for course pages
  - **Payment**: CSS for payment pages

### JavaScript

- **components**: Contains JS for reusable UI components
  - `header.js`: Logic for header
  - `loader.js`: Logic for loading animation
- **pages**: Contains JS for specific pages
  - **Home**: JS for home page
  - **Course**: JS for course pages
  - **Payment**: JS for payment pages
- **utils**: Contains utility functions
  - `site.js`: Common JS for the entire website

### Images

- **logo**: Contains website logos
  - `Main_Logo.jpg`: Main logo of BrainStormEra
- **banners**: Contains website banners
- **courses**: Contains course-related images
- **avatars**: Contains user avatars
- **icons**: Contains icons used throughout the website

## Usage

When adding new files, make sure to place them in the correct directory according to the established structure. This helps keep the project well-organized and easy to maintain.

## Naming Conventions

- Use `kebab-case` for file names (e.g., `header-dropdown.css`)
- Use meaningful names that clearly describe the file's function
- Prefix CSS components with the component name (e.g., `header-dropdown.css`)
- Prefix JS pages with the controller name (e.g., `home-slider.js`)
