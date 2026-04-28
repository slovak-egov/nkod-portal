export type SparqlResult<T> = {
    results: {
        bindings: T[];
    };
};

export type UriColumn = {
    type: 'uri';
    value: string;
};

export type LiteralColumn = {
    type: 'literal';
    value: string;
};

export type DateTimeColumn = LiteralColumn & { datatype: 'http://www.w3.org/2001/XMLSchema#dateTime' };
export type DurationColumn = LiteralColumn & { datatype: 'http://www.w3.org/2001/XMLSchema#duration' };
export type IntColumn = LiteralColumn & { datatype: 'http://www.w3.org/2001/XMLSchema#integer' };
export type DecimalColumn = LiteralColumn & { datatype: 'http://www.w3.org/2001/XMLSchema#decimal' };

export type HarvestationResult = {
    started?: DateTimeColumn | null | undefined;
    ended?: DateTimeColumn | null | undefined;
    duration?: DurationColumn | null | undefined;
    datasetsCount?: IntColumn | null | undefined;
    newDatasetsCount?: IntColumn | null | undefined;
    modifiedDatasetsCount?: IntColumn | null | undefined;
    removedDatasetsCount?: IntColumn | null | undefined;
    catalogsCount?: IntColumn | null | undefined;
    correctlyHarvestedCatalogsCount?: IntColumn | null | undefined;
    unavailableCatalogSourcesCount?: IntColumn | null | undefined;
    publishersCount?: IntColumn | null | undefined;
    externalSourcesCount?: IntColumn | null | undefined;
    catalogSourcesDuration?: DurationColumn | null | undefined;
    catalogSourcesAverageDuration?: DurationColumn | null | undefined;
    catalogSuccessRatio?: DecimalColumn | null | undefined;
    catalogSourcesCount?: IntColumn | null | undefined;
    problemsCount?: IntColumn | null | undefined;
};
