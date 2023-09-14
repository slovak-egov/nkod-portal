import { ReactNode, useCallback } from "react";


import Checkbox from "./Checkbox";
import IdSkModule from "./IdSkModule";

interface IProps<T>
{
    title: ReactNode;
    searchElementTitle: string;
    items: T[];
    getLabel: (item: T) => string;
    getValue: (item: T) => string;
    selectedItems: T[];
    onSelectionChange: (items: T[]) => void;
}

export default function SearchFilter<T>(props: IProps<T>) 
{
    const selectedItems = props.selectedItems;
    const selectedValues = selectedItems.map(props.getValue);
    const onSelectionChange = props.onSelectionChange;
    
    const changeItemSelection = useCallback((item: T, checked: boolean) => {
        onSelectionChange(checked ? [...selectedItems, item] : selectedItems.filter(i => i !== item));
    }, [selectedItems, onSelectionChange]);

    return <IdSkModule moduleType="idsk-search-results-filter">
        <div className="idsk-search-results__filter idsk-search-results-filter__filter-panel">
            <div className="idsk-search-results__link-panel idsk-search-results--expand idsk-search-results__content-type-filter">
                <button className="idsk-search-results__link-panel-button">
                    <span className="idsk-search-results__link-panel__title">
                        {props.title}
                    </span>
                    <span className="idsk-search-results__link-panel--span"></span>
                </button>
                <div className="idsk-search-results__list">
                    <div className="idsk-option-select-filter ">
                        <div className="govuk-form-group">
                            <div className="govuk-checkboxes govuk-checkboxes--small">
                                {props.items.map((item, index) => <Checkbox key={index} label={props.getLabel(item)} checked={selectedValues.includes(props.getValue(item))} onCheckedChange={c => changeItemSelection(item, c)} />)}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </IdSkModule>;
}