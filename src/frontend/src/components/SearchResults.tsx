import { PropsWithChildren, useState } from 'react';
import { Codelist, Facet, RequestQuery, useCodelists, usePublishers } from '../client';
import FormElementGroup from './FormElementGroup';
import GridColumn from './GridColumn';
import GridRow from './GridRow';
import IdSkModule from './IdSkModule';
import Loading from './Loading';
import PageHeader from './PageHeader';
import ResultsCount from './ResultsCount';
import SearchBar from './SearchBar';
import SearchFilter from './SearchFilter';
import SelectElementItems from './SelectElementItems';
import { useTranslation } from 'react-i18next';
import SearchFilterWithQuery from './SearchFilterWithQuery';

type OrderOption = {
    name: string;
    value: string;
};

type FilterValue = {
    id: string;
    label: string;
};

export type Props = {
    header: string;
    query: RequestQuery;
    setQueryParameters: (query: Partial<RequestQuery>) => void;
    orderOptions: OrderOption[];
    filters: string[];
    loading: boolean;
    error: Error | null;
    totalCount: number;
    facets: Facet[];
} & PropsWithChildren;

function PublisherFilter(props: { facet?: Facet; selectedValues: string[]; onChange: (values: string[]) => void }) {
    const [publishers] = usePublishers({ pageSize: -1, orderBy: 'relevance' });
    const { t } = useTranslation();

    if (publishers && publishers.items.length > 0) {
        const facet = props.facet;
        const options: FilterValue[] = [];
        if (facet) {
            const valuesByKey: { [id: string]: string } = {};
            publishers.items.forEach((v) => (valuesByKey[v.key] = v.name));

            const sorted = Object.entries(facet.values)
                .filter((a) => a[1] > 0)
                .sort((a, b) => {
                    const diff = b[1] - a[1];
                    return diff === 0 ? a[0].localeCompare(b[0]) : diff;
                });
            sorted.forEach(([id, count]) => {
                const label = valuesByKey[id];
                if (label) {
                    options.push({
                        id: id,
                        label: label + ' (' + count + ')'
                    });
                }
            });
        } else {
            options.push(...publishers.items.map((v) => ({ id: v.id, label: v.name })));
        }

        if (options.length > 0) {
            return (
                <SearchFilterWithQuery<FilterValue>
                    key="publishers"
                    title={t('publishers')}
                    dataTestId="sr-filter-publishers"
                    searchElementTitle={t('publishers')}
                    items={options}
                    getLabel={(e) => e.label}
                    getValue={(e) => e.id}
                    selectedItems={options.filter((o) => props.selectedValues.includes(o.id))}
                    onSelectionChange={(e) => props.onChange(e.map((o) => o.id))}
                />
            );
        }
    }
    return <></>;
}

function CodelistFilter(props: { codelist: Codelist; facet?: Facet; selectedValues: string[]; onChange: (values: string[]) => void }) {
    const codelist = props.codelist;
    const codelistId = codelist.id;
    const facet = props.facet;
    const options: FilterValue[] = [];
    if (facet) {
        const valuesByKey: { [id: string]: string } = {};
        codelist.values.forEach((v) => (valuesByKey[v.id] = v.label));

        const sorted = Object.entries(facet.values)
            .filter(([_, count]) => count > 0)
            .sort((a, b) => b[1] - a[1]);
        sorted.forEach(([id, count]) => {
            const label = valuesByKey[id];
            if (label) {
                options.push({
                    id: id,
                    label: label + ' (' + count + ')'
                });
            }
        });
    } else {
        options.push(...codelist.values.map((v) => ({ id: v.id, label: v.label })));
    }

    if (options.length > 0) {
        return (
            <SearchFilterWithQuery<FilterValue>
                key={codelistId}
                dataTestId={'sr-filter-' + codelistId}
                title={codelist.label}
                searchElementTitle={codelist.label}
                items={options}
                getLabel={(e) => e.label}
                getValue={(e) => e.id}
                selectedItems={options.filter((o) => props.selectedValues.includes(o.id))}
                onSelectionChange={(e) => props.onChange(e.map((o) => o.id))}
            />
        );
    }
    return <></>;
}

function KeywordFilter(props: { facet?: Facet; selectedValues: string[]; onChange: (values: string[]) => void }) {
    const facet = props.facet;
    const options: FilterValue[] = [];
    const { t } = useTranslation();

    if (facet) {
        const sorted = Object.entries(facet.values)
            .filter(([_, count]) => count > 0)
            .sort((a, b) => b[1] - a[1]);
        sorted.forEach(([id, count]) => {
            const label = id;
            if (label) {
                options.push({
                    id: id,
                    label: label + ' (' + count + ')'
                });
            }
        });
    }

    if (options.length > 0) {
        return (
            <SearchFilterWithQuery<FilterValue>
                key="keyword"
                dataTestId={'sr-filter-keyword'}
                title={t('keywords')}
                searchElementTitle={t('keywords')}
                items={options}
                getLabel={(e) => e.label}
                getValue={(e) => e.id}
                selectedItems={options.filter((o) => props.selectedValues.includes(o.id))}
                onSelectionChange={(e) => props.onChange(e.map((o) => o.id))}
            />
        );
    }
    return <></>;
}

export default function SearchResults(props: Props) {
    const [codelists] = useCodelists(props.filters);
    const { t } = useTranslation();

    const totalCount = props.totalCount ?? 0;
    let pageSize = props.query.pageSize ?? 10;
    if (pageSize <= 0) {
        pageSize = 10;
    }
    const totalPages = Math.ceil(totalCount / pageSize);

    return (
        <IdSkModule moduleType="idsk-search-results" className="idsk-search-results">
            <div className="idsk-search-results__title">
                <PageHeader>{props.header}</PageHeader>
            </div>

            <div className="idsk-search-results__filter-header-panel govuk-grid-column-full idsk-search-results--invisible idsk-search-results--visible__mobile--inline">
                <div className="govuk-heading-xl idsk-search-results--half-width">
                    <span>{t('filters')}</span>
                </div>
                <div className="idsk-search-results--half-width">
                    <button className="idsk-search-results__button--back-to-results" type="button">
                        {t('backToResults')}
                    </button>
                </div>
            </div>

            <GridRow>
                <GridColumn widthUnits={1} totalUnits={4}>
                    <span className="idsk-intro-block__search__span idsk-search-results--invisible__mobile">{t('search')} </span>

                    <div className="idsk-search-results__search-bar">
                        <SearchBar
                            value={props.query.queryText}
                            data-testid="sr-query"
                            onChange={(e) => props.setQueryParameters({ queryText: e.target.value, page: 1 })}
                        />
                    </div>

                    {props.orderOptions.length > 0 ? (
                        <div className="idsk-search-results--order__dropdown">
                            <FormElementGroup
                                label={t('orderBy')}
                                element={(id) => (
                                    <SelectElementItems<OrderOption>
                                        id={id}
                                        data-testid="sr-order"
                                        options={props.orderOptions}
                                        renderOption={(e) => e.name}
                                        getValue={(e) => e.value}
                                        selectedValue={props.query.orderBy ?? props.orderOptions[0].value}
                                        onChange={(o) => props.setQueryParameters({ orderBy: o, page: 1 })}
                                    />
                                )}
                            />
                        </div>
                    ) : null}

                    {props.filters.map((codelistId) => {
                        switch (codelistId) {
                            case 'publishers':
                                return (
                                    <PublisherFilter
                                        key={codelistId}
                                        facet={props.facets.find((f) => f.id === codelistId)}
                                        selectedValues={props.query.filters ? props.query.filters[codelistId] ?? [] : []}
                                        onChange={(v) =>
                                            props.setQueryParameters({
                                                filters: {
                                                    ...props.query.filters,
                                                    [codelistId]: v
                                                },
                                                page: 1
                                            })
                                        }
                                    />
                                );
                            case 'keywords':
                                return (
                                    <KeywordFilter
                                        key={codelistId}
                                        facet={props.facets.find((f) => f.id === codelistId)}
                                        selectedValues={props.query.filters ? props.query.filters[codelistId] ?? [] : []}
                                        onChange={(v) =>
                                            props.setQueryParameters({
                                                filters: {
                                                    ...props.query.filters,
                                                    [codelistId]: v
                                                },
                                                page: 1
                                            })
                                        }
                                    />
                                );
                            default:
                                const codelist = codelists.find((c) => c.id === codelistId);
                                if (codelist) {
                                    return (
                                        <CodelistFilter
                                            codelist={codelist}
                                            facet={props.facets.find((f) => f.id === codelistId)}
                                            key={codelistId}
                                            selectedValues={props.query.filters ? props.query.filters[codelistId] ?? [] : []}
                                            onChange={(v) =>
                                                props.setQueryParameters({
                                                    filters: {
                                                        ...props.query.filters,
                                                        [codelistId]: v
                                                    },
                                                    page: 1
                                                })
                                            }
                                        />
                                    );
                                }
                        }
                        return null;
                    })}
                </GridColumn>
                <GridColumn widthUnits={3} totalUnits={4} className="idsk-search-results__content">
                    {props.loading ? (
                        <Loading />
                    ) : props.error !== null ? (
                        <div>Error: {props.error.message}</div>
                    ) : (
                        <>
                            <GridColumn widthUnits={1} totalUnits={4}>
                                <span className="idsk-search-results__content__number-of-results" data-testid="sr-count">
                                    <ResultsCount count={props.totalCount} />
                                </span>
                            </GridColumn>

                            <GridColumn widthUnits={2} totalUnits={4} className="idsk-search-results__filter-panel--mobile govuk-clearfix">
                                {/* <button className="idsk-search-results__filters__button" title="Filtre">{t('filters')}
                </button>                 */}
                                <div className="idsk-search-results__per-page">
                                    <span>{t('resultsOnPage')}</span>
                                    <div className="govuk-form-group">
                                        <SelectElementItems<number>
                                            id="pageSize"
                                            options={[10, 20, 50]}
                                            renderOption={(e) => e.toString()}
                                            getValue={(e) => e.toString()}
                                            selectedValue={props.query.pageSize.toString()}
                                            onChange={(e) => props.setQueryParameters({ pageSize: Number(e), page: 1 })}
                                        />
                                    </div>
                                </div>
                            </GridColumn>

                            <div className="idsk-search-results--order">
                                <FormElementGroup
                                    label={t('orderBy')}
                                    element={(id) => (
                                        <SelectElementItems<OrderOption>
                                            id={id}
                                            options={props.orderOptions}
                                            renderOption={(e) => e.name}
                                            getValue={(e) => e.value}
                                            selectedValue={props.query.orderBy ?? props.orderOptions[0].value}
                                            onChange={(o) => props.setQueryParameters({ orderBy: o, page: 1 })}
                                        />
                                    )}
                                />
                            </div>

                            <div className="idsk-search-results__page-number--mobile govuk-grid-column-full">
                                {props.query.page > 1 ? (
                                    <button
                                        type="button"
                                        className="idsk-search-results__button--back__mobile"
                                        onClick={() => props.setQueryParameters({ page: props.query.page - 1 })}
                                    >
                                        <svg width="20" height="15" viewBox="0 0 20 15" fill="none" xmlns="http://www.w3.org/2000/svg">
                                            <path
                                                d="M7.44417 14.6753C7.84229 14.2311 7.84229 13.5134 7.44417 13.0691L3.49368 8.63774H18.9792C19.5406 8.63774 20 8.12512 20 7.49858C20 6.87203 19.5406 6.35941 18.9792 6.35941H3.49368L7.45438 1.93943C7.85249 1.49516 7.85249 0.777482 7.45438 0.333207C7.05627 -0.111069 6.41317 -0.111069 6.01506 0.333207L0.298584 6.70116C-0.0995279 7.14543 -0.0995279 7.86311 0.298584 8.30739L6.00485 14.6753C6.40296 15.1082 7.05627 15.1082 7.44417 14.6753Z"
                                                fill="#0065B3"
                                            />
                                        </svg>
                                    </button>
                                ) : null}
                                <span className="idsk-search-results__page-number__mobile"></span>
                                {totalPages > props.query.page ? (
                                    <button
                                        type="button"
                                        className="idsk-search-results__button--forward__mobile"
                                        onClick={() => props.setQueryParameters({ page: props.query.page + 1 })}
                                    >
                                        <svg width="20" height="15" viewBox="0 0 20 15" fill="none" xmlns="http://www.w3.org/2000/svg">
                                            <path
                                                d="M12.5558 0.324663C12.1577 0.768939 12.1577 1.48662 12.5558 1.93089L16.5063 6.36226L1.0208 6.36226C0.45936 6.36226 1.90735e-06 6.87488 1.90735e-06 7.50142C1.90735e-06 8.12797 0.45936 8.64059 1.0208 8.64059L16.5063 8.64059L12.5456 13.0606C12.1475 13.5048 12.1475 14.2225 12.5456 14.6668C12.9437 15.1111 13.5868 15.1111 13.9849 14.6668L19.7014 8.29884C20.0995 7.85457 20.0995 7.13689 19.7014 6.69261L13.9952 0.324663C13.597 -0.108221 12.9437 -0.108221 12.5558 0.324663Z"
                                                fill="#0065B3"
                                            />
                                        </svg>
                                    </button>
                                ) : null}
                            </div>

                            <div className="idsk-search-results__content__picked-filters govuk-grid-column-full idsk-search-results--invisible__mobile">
                                {props.filters.map((codelistId) => {
                                    const codelist = codelists.find((c) => c.id === codelistId);
                                    if (codelist) {
                                        const values = codelist && props.query.filters ? props.query.filters[codelist.id] ?? [] : [];
                                        if (values.length > 0) {
                                            return (
                                                <div
                                                    className="idsk-search-results__content__picked-filters__topics idsk-search-results--invisible"
                                                    key={codelistId}
                                                >
                                                    <span className="idsk-search-results__text">{codelist.label}</span>
                                                </div>
                                            );
                                        }
                                    }
                                    return null;
                                })}
                                <button
                                    className="idsk-search-results__button--turn-filters-off govuk-grid-column-full idsk-search-results--invisible"
                                    type="button"
                                >
                                    {t('removeAllFilters')}
                                </button>
                            </div>

                            <div className="govuk-grid-column-full idsk-search-results__show-results__button idsk-search-results--invisible">
                                <button className="govuk-button idsk-search-results__button-show-results" type="button">
                                    {t('show')}
                                    <ResultsCount count={props.totalCount} />
                                </button>
                            </div>

                            <div className="idsk-search-results__content__all">
                                <div className="idsk-search-results__card govuk-grid-column-full">
                                    <div className="idsk-card idsk-card-basic-variant nkod-search-result-card">{props.children}</div>
                                </div>
                            </div>
                            {totalPages > 0 ? (
                                <div className="idsk-search-results__content__page-changer govuk-grid-column-full">
                                    {props.query.page > 1 ? (
                                        <button
                                            type="button"
                                            className="idsk-search-results__button--back"
                                            onClick={() => props.setQueryParameters({ page: props.query.page - 1 })}
                                            data-testid="sr-previous-page"
                                        >
                                            <svg
                                                className="idsk-search-results__button__svg--previous"
                                                width="20"
                                                height="15"
                                                viewBox="0 -2 25 15"
                                                fill="none"
                                                xmlns="http://www.w3.org/2000/svg"
                                            >
                                                <path
                                                    d="M7.2925 13.8005C7.6825 13.4105 7.6825 12.7805 7.2925 12.3905L3.4225 8.50047H18.5925C19.1425 8.50047 19.5925 8.05047 19.5925 7.50047C19.5925 6.95047 19.1425 6.50047 18.5925 6.50047H3.4225L7.3025 2.62047C7.6925 2.23047 7.6925 1.60047 7.3025 1.21047C6.9125 0.820469 6.2825 0.820469 5.8925 1.21047L0.2925 6.80047C-0.0975 7.19047 -0.0975 7.82047 0.2925 8.21047L5.8825 13.8005C6.2725 14.1805 6.9125 14.1805 7.2925 13.8005Z"
                                                    fill="#0065B3"
                                                ></path>
                                            </svg>
                                            {t('showPrevious')}
                                        </button>
                                    ) : null}
                                    {totalPages > props.query.page ? (
                                        <button
                                            type="button"
                                            className="idsk-search-results__button--forward"
                                            onClick={() => props.setQueryParameters({ page: props.query.page + 1 })}
                                            data-testid="sr-next-page"
                                        >
                                            {t('showNext')}
                                            <svg
                                                className="idsk-search-results__button__svg--next"
                                                width="20"
                                                height="13"
                                                viewBox="-5 0 25 13"
                                                fill="none"
                                                xmlns="http://www.w3.org/2000/svg"
                                            >
                                                <path
                                                    d="M12.5558 0.281376C12.1577 0.666414 12.1577 1.2884 12.5558 1.67344L16.5063 5.51395L1.0208 5.51395C0.45936 5.51395 1.90735e-06 5.95823 1.90735e-06 6.50123C1.90735e-06 7.04424 0.45936 7.48851 1.0208 7.48851L16.5063 7.48851L12.5456 11.3192C12.1475 11.7042 12.1475 12.3262 12.5456 12.7112C12.9437 13.0963 13.5868 13.0963 13.9849 12.7112L19.7014 7.19233C20.0995 6.80729 20.0995 6.1853 19.7014 5.80027L13.9952 0.281376C13.597 -0.0937901 12.9437 -0.0937901 12.5558 0.281376Z"
                                                    fill="#0065B3"
                                                ></path>
                                            </svg>
                                        </button>
                                    ) : null}

                                    <div className="idsk-search-results__page-number govuk-grid-column-full">
                                        <span data-lines="Strana $value1 z $value2" data-testid="sr-current-page">
                                            {t('page')} {props.query.page} z {totalPages}
                                        </span>
                                    </div>
                                </div>
                            ) : null}
                        </>
                    )}
                </GridColumn>
            </GridRow>
        </IdSkModule>
    );
}
