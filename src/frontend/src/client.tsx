import axios, { AxiosResponse } from "axios";
import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router";
import { useSearchParams } from "react-router-dom";

const baseUrl = process.env.REACT_APP_API_URL;

export type Publisher = {
    id: string;
    key: string;
    name: string;
    datasetCount: number;
    themes: { [id: string] : number }|null
}

type Temporal = {
    startDate: string|null;
    endDate: string|null;
}

type CardView = {
    name: string|null;
    email: string|null;
}

type SaveResult = {
    id: string;
    success: boolean;
    errors: { [id: string] : string }
}

export type CodelistValue = {
    id: string;
    label: string;
}

export type LanguageDependentTexts = {
    [id: string]: string
}

export type Language = {
    id: string;
    name: string;
}

export type DatasetInput = {
    id: string;
    isPublic: boolean;
    name: LanguageDependentTexts;
}

export type Dataset = {
    id: string;
    isPublic: boolean;
    name: string|null;
    description: string|null;
    publisherId: string|null;
    publisher: Publisher|null;
    themes: string[];
    themeValues: CodelistValue[];
    accrualPeriodicity: string|null;
    accrualPeriodicityValue: CodelistValue|null;
    keywords: string[];
    type: string|null;
    typeValue: CodelistValue|null;
    spatial: string[];
    spatialValues: CodelistValue[];
    temporal: Temporal|null;
    contactPoint: CardView|null;
    documentation: string|null;
    specification: string|null;
    euroVocThemes: string[];
    euroVocThemeValues: CodelistValue[];
    spatialResolutionInMeters: number|null;
    temporalResolution: string|null;
    isPartOf: string|null;
    distributions: Distribution[];
}

type TermsOfUse = {
    authorsWorkType: string|null;
    originalDatabaseType: string|null;
    databaseProtectedBySpecialRightsType: string|null;
    personalDataContainmentType: string|null;
}

type Distribution = {
    id: string;
    termsOfUse: TermsOfUse|null;
    downloadUrl: string|null;
    accessUrl: string|null;
    format: string|null;
    formatValue: CodelistValue|null;
    mediaType: string|null;
    conformsTo: string|null;
    compressFormat: string|null;
    packageFormat: string|null;
    title: string|null;
}

type LocalCatalog = {
    id: string;
    name: string;
    description: string|null;
    publisher: Publisher|null;
    contactPoint: CardView|null;
    homePage: string|null;
}

export type Codelist = {
    id: string;
    label: string;
    values: CodelistValue[];
}

export type Facet = {
    id: string;
    values: { [id: string] : number };
}

type Response<T> = {
    items: T[];
    totalCount: number;
    facets: Facet[];
}

export type RequestQuery = {
    pageSize: number;
    page: number;
    queryText?: string;
    language: string;
    orderBy?: string;
    filters?: { [id: string] : string[] };
    requiredFacets: string[];
}

export function useDelay<T>(initialValue: T, delay: number, callback: (value: T) => void) {
    const [value, setValue] = useState<T>(initialValue);

    useEffect(() => {
        const timeout = setTimeout(() => callback(value), delay);
        return () => clearTimeout(timeout);
    }, [value, callback, delay]);

    return [value, setValue] as const;
}

export function useEntities<T>(url: string, initialQuery?: Partial<RequestQuery>) {
    const [searchParams] = useSearchParams();

    let defaultParams: RequestQuery = {
        pageSize: 10,
        page: 1,
        queryText: '',
        language: 'sk',
        orderBy: 'name',
        filters: {},
        requiredFacets: [],
    };

    if (searchParams.has('publisher')) {
        defaultParams = {
            ...defaultParams,
            filters: {
                ...defaultParams.filters,
                publishers: [searchParams.get('publisher')!]
            }
        }
    }

    if (searchParams.has('query')) {
        defaultParams = {
            ...defaultParams,
            queryText: searchParams.get('query')!
        }
    }

    const [query, setQuery] = useState<RequestQuery>({
      ...defaultParams,
        ...initialQuery
    });
    const [items, setItems] = useState<Response<T>|null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        async function load() {
            setLoading(true);
            if (query.page > 0) {
                try{
                    const response: AxiosResponse<Response<T>> = await axios.post(url, query);
                    setItems(response.data);
                } catch (err) {
                    if (err instanceof Error) {
                        setError(err);
                    }
                    setItems(null);
                } finally {
                    setLoading(false);
                }
            }
        }

        load();
    }, [query, url]);

    const setQueryParameters = useCallback((query: Partial<RequestQuery>) => {
        setQuery(q => ({...q, ...query}));
    }, []);

    return [items, query, setQueryParameters, loading, error] as const;
}

export function useEntity<T>(url: string) {
    const { id } = useParams();
    const [item, setItem] = useState<T|null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        async function load() {
            if (id) {
                const query: RequestQuery = {
                    language: 'sk',
                    page: 1,
                    pageSize: 1,
                    filters: {
                        id: [id]
                    },
                    requiredFacets: []
                }
    
                setLoading(true);
                setError(null);
                setItem(null);
                try{
                    const response: AxiosResponse<Response<T>> = await axios.post(url, query);
                    if (response.data.items.length > 0) {
                        setItem(response.data.items[0]);
                    }
                } catch (err) {
                    if (err instanceof Error) {
                        setError(err);
                    }
                } finally {
                    setLoading(false);
                }
            }
        }

        load();
    }, [id, url]);

    return [ item, loading, error ] as const;
}

export function useDataset() {
    return useEntity<Dataset>(baseUrl + 'datasets/search');
}

export function useLocalCatalog() {
    return useEntity<LocalCatalog>(baseUrl + 'local-catalogs/search');
}

export function useDatasets(initialQuery?: Partial<RequestQuery>) {
    return useEntities<Dataset>(baseUrl + 'datasets/search', {orderBy: 'created', ...initialQuery});
}

export function useLocalCatalogs(initialQuery?: Partial<RequestQuery>) {
    return useEntities<LocalCatalog>(baseUrl + 'local-catalogs/search', initialQuery);
}

export function usePublishers(initialQuery?: Partial<RequestQuery>) {
    return useEntities<Publisher>(baseUrl + 'publishers/search', {orderBy: 'relevance', ...initialQuery});
}

export function useCodelists(keys: string[]) {
    const [codelists, setCodelists] = useState<Codelist[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        async function load() {
            setLoading(true);
            setError(null);
            setCodelists([]);
            try{
                const response: AxiosResponse<Codelist[]> = await axios.get(baseUrl + 'codelists', {
                    params: {
                        keys: keys
                    }
                });
                if (response.data.length > 0) {
                    setCodelists(response.data);
                }
            } catch (err) {
                if (err instanceof Error) {
                    setError(err);
                }
            } finally {
                setLoading(false);
            }
        }

        load();
    }, [keys]);

    return [ codelists, loading, error ] as const;
}

export function useEntityAdd<T>(url: string, initialValue: T) {
    const [entity, setEntity] = useState<T>(initialValue);
    const [errors, setErrors] = useState<{[id: string]: string}>({});
    const [saving, setSaving] = useState(false);
    const [saveResult, setSaveResult] = useState<SaveResult|null>(null);

    const save = useCallback(async () => {
        setSaving(true);
        setErrors({});
        try {
            const response: AxiosResponse<SaveResult> = await axios.post(url, entity);
            setErrors(response.data.errors);
            setSaveResult(response.data);
            return response.data;
        } catch (err) {
            if (err instanceof Error) {
                setErrors({
                    generic: err.message
                });
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [entity, url]);

    const setEntityProperties = useCallback((properties: Partial<T>) => {
        setEntity({...entity, ...properties});
    }, [entity, setEntity]);

    return [ entity, setEntityProperties, errors, saving, saveResult, save ] as const;
}

export function useDatasetAdd(initialValue: DatasetInput) {
    return useEntityAdd<DatasetInput>(baseUrl + 'datasets', initialValue);
}

export const supportedLanguages: Language[] = [
    {
        id: 'sk',
        name: 'slovensky'
    }
]