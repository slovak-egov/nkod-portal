import { ReactNode, useEffect, useState } from "react";
import GridColumn from "./GridColumn";
import GridRow from "./GridRow";
import BaseInput from "./BaseInput";

type Props<T> = 
{
    getOptions: (query: string) => Promise<T[]>;
    selectedOptions: T[];
    onChange: (items: string[]) => void;
    renderOption: (item: T) => ReactNode;
    getValue: (item: T) => string;
    id: string;
}

export default function MultiTextBoxAutocomplete<T>(props: Props<T>)
{
    const [query, setQuery] = useState<string>('');
    const [options, setOptions] = useState<T[]>([]);
    const { getOptions, selectedOptions, onChange, renderOption, getValue, ...rest } = props;

    useEffect(()=> {
        async function loadOptions() {
            const q = query.trim();
            if (q !== '') {
                const options = await getOptions(q);
                setOptions(options);
            } else {
                setOptions([]);
            }
        }

        loadOptions();
    }, [query, getOptions]);

    return <><GridRow>
        <GridColumn widthUnits={4} totalUnits={4}>
            <div style={{position: 'relative'}}>
                <BaseInput value={query} onChange={e => setQuery(e.target.value)} {...rest} />
                {options.length > 0 ? <div style={{position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 1000, backgroundColor: 'white', border: '1px solid #b1b4b6', borderTop: 'none', maxHeight: '200px', 'padding': '10px', overflowY: 'scroll'}}>
                    {options.map(o => <div key={getValue(o)} className="govuk-body">
                        <div onClick={() => {
                            const v = getValue(o);
                            const list = selectedOptions.map(v => getValue(v));
                            if (!list.includes(v)) {
                                onChange([...list, v]);
                            }
                            setQuery('');
                        }}>
                            {renderOption(o)}
                        </div>
                    </div>)}
                </div> : null}
            </div>
        </GridColumn>
    </GridRow>
    <div>
        {selectedOptions.length > 0 ? <div className="nkod-entity-detail"><div className="nkod-entity-detail-tags govuk-clearfix" style={{marginTop: '20px'}}>
                {selectedOptions.map(o => <div key={getValue(o)} className="govuk-body nkod-entity-detail-tag" style={{cursor: 'pointer'}} onClick={() => {
                    onChange(selectedOptions.map(getValue).filter(x => x !== getValue(o)));
                }}>
                    <span>
                    {renderOption(o)} <span style={{marginLeft: '10px'}}>x</span>
                    </span>
                </div>)}
            </div></div> : null}
    </div></>
}