module.exports = {
  siteMetadata: {
    title: `Spark FHIR server | Documentation`,
    description: `C# Reference Implementation for HL7 FHIR`,
    author: `Spark`,
  },
  pathPrefix: "/spark",
  plugins: [
    {
      resolve: `gatsby-theme-docz`,
      options: {
        themeConfig: {
          mode: `dark`,
        },
        ignore: ['README.md', 'LICENSE.md'],
      },
    },
    `gatsby-plugin-react-helmet`,
    {
      resolve: `gatsby-source-filesystem`,
      options: {
        name: `images`,
        path: `${__dirname}/src/images`,
      },
    },
    `gatsby-transformer-sharp`,
    `gatsby-plugin-sharp`,
    {
      resolve: `gatsby-plugin-manifest`,
      options: {
        name: `gatsby-starter-default`,
        short_name: `starter`,
        start_url: `/`,
        background_color: `#663399`,
        theme_color: `#663399`,
        display: `minimal-ui`,
        icon: `src/images/gatsby-icon.png`, // This path is relative to the root of the site.
      },
    },
  ],
}
