// Used for the production build.
// The addresses are relative: the web server (nginx) passes them on to the API.
export const environment = {
  apiUrl: '/api',
  hubUrl: '/hubs/comments',
  // Put in front of file addresses like "/uploads/<name>". Empty: the files come from the same address.
  filesUrl: '',
};
