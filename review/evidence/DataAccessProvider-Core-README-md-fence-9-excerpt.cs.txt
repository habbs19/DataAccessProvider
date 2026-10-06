var dataSourceFactory = serviceProvider.GetService<IDataSourceFactory>();

// Register the custom data source
dataSourceFactory.RegisterDataSource<XmlFileSourceParams, XmlFileSource>();

