// Production build. The demo runs both APIs locally, so the URLs match development; a deployment would point these
// at the real hosts. No secrets ever go here: this file ships to the browser.
export const environment = {
  production: true,
  identityApiUrl: 'http://localhost:5001/api',
  expensesApiUrl: 'http://localhost:5002/api',
};
