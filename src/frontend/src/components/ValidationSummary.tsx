import { useId } from "react";
import IdSkModule from "./IdSkModule";

interface IProps
{
    title?: string;
    description?: string;
    elements: IErrorElementReference[];
}

interface IErrorElementReference
{
    elementId: string;
    message: string;
}

export default function ValidationSummary(props: IProps)
{
    const id = useId();

    return <IdSkModule moduleType="govuk-error-summary" className="govuk-error-summary optional-extra-class" role="alert" tabIndex={-1} aria-labelledby={props.title ? id + '-summary-title' : ''}>
        {props.title ? <h2 className="govuk-error-summary__title" id={id + '-summary-title'}>
            {props.title}
        </h2> : null}
        <div className="govuk-error-summary__body">
            {props.description && <p>{props.description}</p>}
            {props.elements.length > 0 && <ul className="govuk-list govuk-error-summary__list">
                {props.elements.map(e => <li key={e.elementId}>
                    <a href={'#' + e.elementId}>{e.message}</a>
                </li>)}
            </ul>}
        </div>
    </IdSkModule>
}

ValidationSummary.defaultProps = {
    elements: []
};