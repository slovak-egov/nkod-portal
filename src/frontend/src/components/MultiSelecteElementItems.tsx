import { ReactNode, useState } from "react";
import SelectElementItems from "./SelectElementItems";
import Button from "./Button";

type Props<T> = 
{
    options: T[];
    selectedOptions: T[];
    onChange: (items: string[]) => void;
    renderOption: (item: T) => ReactNode;
    getValue: (item: T) => string;
    id: string;
}

type OptionalValue = {
    id: string,
    label: ReactNode
}

export default function MultiSelectElementItems<T>(props: Props<T>)
{
    const [newValue, newValueChanged] = useState<string>('');
    const { options, selectedOptions, onChange, renderOption, getValue, ...rest } = props;

    return <><div>
        <SelectElementItems<OptionalValue> {...rest} options={[{id: '', label: 'zvoľte zo zoznamu'}, ...options.map(v => ({id: getValue(v), label: renderOption(v)}))]} selectedValue={newValue} onChange={newValueChanged} renderOption={v => v.label} getValue={v => v.id} />
        <Button buttonType="secondary" onClick={() => {
            if (newValue !== '') {
                const list = selectedOptions.map(v => getValue(v));
                if (!list.includes(newValue)) {
                    onChange([...list, newValue]);
                }
            }}} disabled={newValue === ''}>
            Pridať do zoznamu
        </Button>
    </div>
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