import Radio from "./Radio";

type Props<T> =
{
    options: T[];
    selectedOption: T;
    onChange: (item: T) => void;
    renderOption: (item: T) => string;
    getValue: (item: T) => string;
    label: string;
    id: string;
    hint?: string;
    errorMessage?: string;
    inline?: boolean;
    disabled?: boolean;
}

export default function MultiRadio<T>(props: Props<T>)
{
    const {options, inline, selectedOption, onChange, renderOption, getValue, label, hint, errorMessage, id, ...inputProperties} = props;

    return <div className={'govuk-form-group ' + (errorMessage ? 'govuk-form-group--error' : '')}>
        <fieldset className="govuk-fieldset">
            <legend className="govuk-fieldset__legend">
                {label}
            </legend>
            {hint ? <span className="govuk-hint">{hint}</span> : null}
            {errorMessage ? <span className="govuk-error-message"><span className="govuk-visually-hidden">Chyba: </span> {errorMessage}</span> : null}
            <div className={'govuk-radios ' + (inline === true ? 'govuk-radios--inline' : '')}>
                {options.map((item, index) => <Radio label={renderOption(item)} key={index} checked={selectedOption ? getValue(selectedOption) === getValue(item) : false} {...inputProperties} onChange={e => {
                    if (e.target.checked)
                    {
                        onChange(item);
                    }
                }} />)}
            </div>
        </fieldset>
    </div>
}

MultiRadio.defaultProps = {
    hint: undefined,
    errorMessage: undefined
}