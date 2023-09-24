import { ReactNode, useId } from "react";

type Props =
{
    label: string;
    hint?: string;
    element: (id: string) => ReactNode;
    errorMessage?: string;
}

export default function FormElementGroup(props: Props)
{
    const id = useId();

    return <div className={'govuk-form-group ' + (props.errorMessage ? 'govuk-form-group--error' : '')}>
        <label className="govuk-label" htmlFor={id}>
            {props.label}
        </label>
        {props.hint && <span className="govuk-hint">{props.hint}</span>}
        {props.errorMessage && <span className="govuk-error-message"><span className="govuk-visually-hidden">Chyba: </span> {props.errorMessage}</span>}
        {props.element(id)}
    </div>
}

FormElementGroup.defaultProps = {
    hint: undefined,
    errorMessage: undefined
}