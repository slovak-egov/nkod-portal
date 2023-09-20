import axios, { AxiosError, AxiosResponse } from "axios";
import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router";
import { useSearchParams } from "react-router-dom";

const baseUrl = process.env.REACT_APP_API_URL;

let curentToken: string|null = null;

export const knownCodelists = {
    'dataset' : {
        'theme': 'http://publications.europa.eu/resource/authority/data-theme',
        'type': 'https://data.gov.sk/set/codelist/dataset-type',
        'accrualPeriodicity': 'http://publications.europa.eu/resource/authority/frequency',
        'spatial': 'http://publications.europa.eu/resource/authority/place',
        'euroVoc': 'http://eurovoc.europa.eu/100141',
    },
    'distribution': {
        'authorsWorkType': 'https://data.gov.sk/set/codelist/authors-work-type',
        'originalDatabaseType': 'https://data.gov.sk/set/codelist/original-database-type',
        'databaseProtectedBySpecialRightsType': 'https://data.gov.sk/set/codelist/database-creator-special-rights-type',
        'personalDataContainmentType': 'https://data.gov.sk/set/codelist/personal-data-occurence-type',
        'format': 'http://publications.europa.eu/resource/authority/file-type',
        'mediaType': 'http://www.iana.org/assignments/media-types',
    }
}

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

export type LanguageDependentTextsMulti = {
    [id: string]: string[]
}

export type Language = {
    id: string;
    name: string;
}

export type UserInfo = {
    publisher: string|null;
    publisherView: Publisher;
}

export type DatasetInput = {
    id?: string;
    isPublic: boolean;
    name: LanguageDependentTexts;
    description: LanguageDependentTexts;
    themes: string[];
    accrualPeriodicity: string|null;
    keywords: LanguageDependentTextsMulti;
    type: string[];
    spatial: string[];
    startDate: string|null;
    endDate: string|null;
    contactName: LanguageDependentTexts;
    contactEmail: string|null;
    documentation: string|null;
    specification: string|null;
    euroVocThemes: string[];
    spatialResolutionInMeters: number|null;
    temporalResolution: string|null;
    isPartOf: string|null;
}

export type DistributionInput = {
    id?: string;
    datasetId: string|null;
    authorsWorkType: string|null;
    originalDatabaseType: string|null;
    databaseProtectedBySpecialRightsType: string|null;
    personalDataContainmentType: string|null;
    downloadUrl: string|null;
    accessUrl: string|null;
    format: string|null;
    mediaType: string|null;
    conformsTo: string|null;
    compressFormat: string|null;
    packageFormat: string|null;
    title: LanguageDependentTexts|null;
    fileId: string|null;
}

export type LocalCatalogInput = {
    id?: string;
    isPublic: boolean;
    name: LanguageDependentTexts;
    description: LanguageDependentTexts;
    contactName: LanguageDependentTexts;
    contactEmail: string|null;
    homePage: string|null;
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
    type: string[];
    typeValues: CodelistValue|null;
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

export type Distribution = {
    id: string;
    datasetId: string|null;
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

export type LocalCatalog = {
    id: string;
    isPublic: boolean;
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

    const refresh = useCallback(async () => {
        setLoading(true);
        if (query.page > 0) {
            try{
                const response: AxiosResponse<Response<T>> = await axios.post(url, query, {
                    headers: curentToken ? {
                        'Authorization': 'Bearer ' + curentToken
                    } : {}
                });
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
    }, [query, url]);

    useEffect(() => {
        refresh();
    }, [refresh]);

    const setQueryParameters = useCallback((query: Partial<RequestQuery>) => {
        setQuery(q => ({...q, ...query}));
    }, []);

    return [items, query, setQueryParameters, loading, error, refresh] as const;
}

export function useEntity<T>(url: string, sourceId?: string) {
    const { id } = useParams();
    const [item, setItem] = useState<T|null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const targetId = sourceId ?? id;

    useEffect(() => {
        async function load() {
            if (targetId) {
                const query: RequestQuery = {
                    language: 'sk',
                    page: 1,
                    pageSize: 1,
                    filters: {
                        id: [targetId]
                    },
                    requiredFacets: []
                }
    
                setLoading(true);
                setError(null);
                setItem(null);
                try{
                    const response: AxiosResponse<Response<T>> = await axios.post(url, query, {
                        headers: curentToken ? {
                            'Authorization': 'Bearer ' + curentToken
                        } : {}
                    });
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

export function useDataset(id?: string) {
    return useEntity<Dataset>(baseUrl + 'datasets/search', id);
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

export function useDistributions(initialQuery?: Partial<RequestQuery>) {
    return useEntities<Distribution>(baseUrl + 'distributions/search', {orderBy: 'relevance', ...initialQuery});
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

export async function searchCodelistItem(codelistId: string, query: string) {
    const response: AxiosResponse<Codelist[]> = await axios.post(baseUrl + 'codelists/search', {}, {
        params: {
            key: codelistId,
            query: query
        }
    });
    return response.data;
}

export async function getCodelistItem(codelistId: string, id: string) {
    const response: AxiosResponse<CodelistValue> = await axios.get(baseUrl + 'codelists/item', {
        params: {
            key: codelistId,
            id: id
        }
    });
    if (response.status === 200) {
        return response.data;
    }
    return null;
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
            const response: AxiosResponse<SaveResult> = await axios.post(url, entity, {
                headers: curentToken ? {
                    'Authorization': 'Bearer ' + curentToken
                } : {}
            });
            setErrors(response.data.errors);
            setSaveResult(response.data);
            return response.data;
        } catch (err) {
            if (err instanceof AxiosError) {
                setErrors(err.response?.data.errors ?? {'generic': 'Error'});
            } else if (err instanceof Error) {
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

export function useEntityEdit<TEntity, TInput>(url: string, loadUrl: string, initialValue: (entity: TEntity) => TInput) {
    const [entity, setEntity] = useState<TInput|null>(null);
    const [errors, setErrors] = useState<{[id: string]: string}>({});
    const [saving, setSaving] = useState(false);
    const [saveResult, setSaveResult] = useState<SaveResult|null>(null);
    const [item, loading, error] = useEntity<TEntity>(loadUrl);

    useEffect(() => {
        if (item) {
            setEntity(initialValue(item));
        }
    }, [item, initialValue]);

    const save = useCallback(async () => {
        setSaving(true);
        setErrors({});
        try {
            const response: AxiosResponse<SaveResult> = await axios.put(url, entity, {
                headers: curentToken ? {
                    'Authorization': 'Bearer ' + curentToken
                } : {}
            });
            setErrors(response.data.errors);
            setSaveResult(response.data);
            return response.data;
        } catch (err) {
            if (err instanceof AxiosError) {
                setErrors(err.response?.data.errors ?? {'generic': 'Error'});
            } else if (err instanceof Error) {
                setErrors({
                    generic: err.message
                });
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [entity, url]);

    const setEntityProperties = useCallback((properties: Partial<TInput>) => {
        if (entity) {
            setEntity({...entity, ...properties});
        }
    }, [entity, setEntity]);

    return [ entity, item, loading, setEntityProperties, errors, saving, saveResult, save ] as const;
}

export function useDatasetAdd(initialValue: DatasetInput) {
    return useEntityAdd<DatasetInput>(baseUrl + 'datasets', initialValue);
}

export function useDatasetEdit(initialValue: (entity: Dataset) => DatasetInput) {
    return useEntityEdit<Dataset, DatasetInput>(baseUrl + 'datasets', baseUrl + 'datasets/search', initialValue);
}

export function useDistributionAdd(initialValue: DistributionInput) {
    return useEntityAdd<DistributionInput>(baseUrl + 'distributions', initialValue);
}

export function useDistributionEdit(initialValue: (entity: Distribution) => DistributionInput) {
    return useEntityEdit<Distribution, DistributionInput>(baseUrl + 'distributions', baseUrl + 'distributions/search', initialValue);
}

export function useLocalCatalogAdd(initialValue: LocalCatalogInput) {
    return useEntityAdd<LocalCatalogInput>(baseUrl + 'local-catalogs', initialValue);
}

export function useLocalCatalogEdit(initialValue: (entity: LocalCatalog) => LocalCatalogInput) {
    return useEntityEdit<LocalCatalog, LocalCatalogInput>(baseUrl + 'local-catalogs', baseUrl + 'local-catalogs/search', initialValue);
}

export const supportedLanguages: Language[] = [
    {
        id: 'sk',
        name: 'slovensky'
    }
]

export function useUserInfo() {
    const [userInfo, setUserInfo] = useState<UserInfo|null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        async function load() {
            if (userInfo === null) {
                setLoading(true);
                setError(null);
                setUserInfo(null);
                try{
                    const response: AxiosResponse<UserInfo> = await axios.post(baseUrl + 'user-info', {}, {
                        headers: curentToken ? {
                            'Authorization': 'Bearer ' + curentToken
                        } : {}
                    });
                    setUserInfo(response.data);
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
    }, [userInfo]);

    return [userInfo, loading, error] as const;
}

export function extractLanguageErrors(errors: {[id: string]: string}, key: string) {
    const filtered: {[id: string]: string} = {};
    for (const [k, v] of Object.entries(errors)) {
        if (k.startsWith(key )) {
            filtered[k.substring(key.length)] = v;
        }
    }
    return filtered;
}

export async function removeEntity(url: string, id: string) {
    if (window.confirm('Skutočne chcete odstrániť záznam?')) {
        try{
            await axios.delete(url, {
                headers: curentToken ? {
                    'Authorization': 'Bearer ' + curentToken
                } : {},
                params: {
                    id: id
                }
            });
            return true;
        } catch (err) {
            alert(err);
            return false;
        }
    }
}

type FileUploadResult = {
    id: string;
    url: string;
}

export function removeDataset(id: string) {
    return removeEntity(baseUrl + 'datasets', id);
}

export function removeDistribution(id: string) {
    return removeEntity(baseUrl + 'distributions', id);
}

export function removeLocalCatalog(id: string) {
    return removeEntity(baseUrl + 'local-catalogs', id);
}

export function useSingleFileUpload() {
    const [uploading, setUploading] = useState(false);

    const upload = useCallback(async (file: File) => {
        const formData = new FormData();
        formData.append('file', file, file.name);
        setUploading(true);
        try{
            const response: AxiosResponse<FileUploadResult> =  await axios.post(baseUrl + 'upload', formData, {
                headers: {
                    'Authorization': 'Bearer ' + process.env.REACT_APP_TOKEN
                }
            });
            return response.data;
        } finally {
            setUploading(false);
        }
    }, []);

    return [ uploading, upload ] as const;
}