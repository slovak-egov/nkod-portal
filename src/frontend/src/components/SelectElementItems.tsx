import { ReactNode } from "react";
import SelectElement from "./SelectElement";

type Props<T> = 
{
    options: T[];
    selectedValue: string;
    onChange: (item: string) => void;
    renderOption: (item: T) => ReactNode;
    getValue: (item: T) => string;
    id: string;
}

export default function SelectElementItems<T>(props: Props<T>)
{
    const { options, selectedValue, onChange, renderOption, getValue, ...rest } = props;

    return <SelectElement onChange={e => onChange(getValue(options[e.target.selectedIndex]))} value={selectedValue} {...rest}>
        {options.map(option => {
            const v = getValue(option);
            return <option key={v} value={v}>{renderOption(option)}</option>
        })}
    </SelectElement>;
}