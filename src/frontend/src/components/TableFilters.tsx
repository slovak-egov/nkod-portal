import { ReactNode } from 'react';
import GridColumn from './GridColumn';
import GridRow from './GridRow';
import FormElementGroup from './FormElementGroup';
import Select from 'react-select/dist/declarations/src/Select';
import BaseInput from './BaseInput';

export type FilterDefinition = { id: string; value: string };

export type FilterSection = {
    label: ReactNode | string;
    filters: Filter[];
};

type BaseFilter = {
    id: string;
    label: string;
};

export type SelectOption = {
    value: string;
    label: ReactNode | string;
    disabled?: boolean;
};

export type Filter =
    | (BaseFilter & {
          type: 'text';
      })
    | (BaseFilter & {
          type: 'date';
      })
    | (BaseFilter & { type: 'select'; options: SelectOption[] });

function isFilterSectionArray(items: Filter[] | FilterSection[]): items is FilterSection[] {
    return items.length > 0 && 'filters' in items[0];
}

type FilterProps = {
    filters: Filter[] | FilterSection[];
    filterValues: FilterDefinition[];
    onFilter: (filter: FilterDefinition[]) => void;
};

export type Props = FilterProps;

function Filter({ filter, filterValues, onFilter }: { filterValues?: FilterDefinition[]; onFilter?: (filter: FilterDefinition[]) => void; filter: Filter }) {
    const multiFilterValue = filterValues?.find((f) => f.id === filter.id)?.value;
    const filterValue = Array.isArray(multiFilterValue) ? multiFilterValue[0] : multiFilterValue;

    const updateFilter = (value: string | null | undefined) => {
        if (onFilter) {
            const newFilters = filterValues?.filter((f) => f.id !== filter.id) ?? [];
            if (value !== null && value !== undefined) {
                newFilters.push({
                    id: filter.id,
                    value: value
                });
            }
            onFilter(newFilters);
        }
    };

    return (
        <GridColumn widthUnits={1} totalUnits={3} from="desktop">
            {filter.type === 'text' ? (
                <FormElementGroup
                    label={filter.label}
                    element={(id) => <BaseInput id={id} value={filterValue ?? ''} onChange={(e) => updateFilter(e.target.value)} />}
                />
            ) : filter.type === 'select' ? (
                <FormElementGroup
                    label={filter.label}
                    element={(id) => (
                        <select id={id} value={filterValue ?? filter.options[0]?.value} onChange={(e) => updateFilter(e.target.value)} className="govuk-select">
                            {filter.options.map((option) => (
                                <option key={option.value} value={option.value}>
                                    {option.label}
                                </option>
                            ))}
                        </select>
                    )}
                />
            ) : null}
        </GridColumn>
    );
}

export default function TableFilters({ filters, filterValues, onFilter }: Props) {
    return filters && filters.length > 0 ? (
        <div data-module="idsk-table-filter" className="idsk-table-filter ">
            <>
                <div className="idsk-table-filter__panel idsk-table-filter__inputs">
                    <div className="idsk-table-filter__title govuk-heading-m">Filtrovať obsah</div>
                    <button
                        tabIndex={0}
                        className="govuk-body govuk-link idsk-filter-menu__toggle"
                        data-open-text="Rozbaliť obsah filtra"
                        data-close-text="Zbaliť obsah filtra"
                        data-category-name=""
                        aria-label="Rozbaliť obsah filtra"
                        type="button"
                    >
                        Rozbaliť obsah filtra
                    </button>

                    <form className="idsk-table-filter__content" aria-hidden="true" action="#">
                        {isFilterSectionArray(filters) ? (
                            filters.map((f, i) => (
                                <div className="idsk-table-filter__category" key={i}>
                                    <div className="idsk-table-filter__title govuk-heading-s">{f.label}</div>
                                    <button
                                        className="govuk-body govuk-link idsk-filter-menu__toggle"
                                        tabIndex={-1}
                                        data-open-text="Rozbaliť sekciu filtra"
                                        data-close-text="Zbaliť sekciu filtra"
                                        data-category-name="Podľa miesta"
                                        aria-label="Rozbaliť sekciu filtra Podľa miesta"
                                        type="button"
                                    >
                                        Rozbaliť sekciu filtra
                                    </button>
                                    <div className="idsk-table-filter__content">
                                        <GridRow className="idsk-table-filter__filter-inputs">
                                            {f.filters.map((f) => (
                                                <Filter key={f.id} filter={f} onFilter={onFilter} filterValues={filterValues} />
                                            ))}
                                        </GridRow>
                                    </div>
                                </div>
                            ))
                        ) : (
                            <div className="govuk-grid-row idsk-table-filter__filter-inputs">
                                {filters.map((f) => (
                                    <Filter key={f.id} filter={f} onFilter={onFilter} filterValues={filterValues} />
                                ))}
                            </div>
                        )}

                        <button type="submit" className="idsk-button submit-table-filter" disabled={true} style={{ display: 'none' }}>
                            Filtrovať (<span className="count">0</span>)
                        </button>
                    </form>
                </div>

                <div
                    className="idsk-table-filter__panel idsk-table-filter__active-filters idsk-table-filter__active-filters__hide idsk-table-filter--expanded"
                    data-remove-filter="Zrušiť filter"
                    data-remove-all-filters="Zrušiť všetko"
                    style={{ display: 'none' }}
                >
                    <div className="govuk-body idsk-table-filter__title">Aktívny filter</div>
                    <button
                        className="govuk-body govuk-link idsk-filter-menu__toggle"
                        tabIndex={0}
                        data-open-text="Rozbaliť aktívny filter"
                        data-close-text="Zbaliť aktívny filter"
                        data-category-name=""
                        aria-label="Zbaliť aktívny filter"
                        type="button"
                    >
                        Zbaliť aktívny filter
                    </button>

                    <div className="govuk-clearfix idsk-table-filter__content"></div>
                </div>
            </>
        </div>
    ) : null;
}
