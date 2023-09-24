type Props =
{
    tag: string;
    description: string;
}

export default function PhaseBanner(props: Props)
{
    return <div className="govuk-phase-banner">
        <p className="govuk-phase-banner__content">
            <strong className="govuk-tag govuk-phase-banner__content__tag">{props.tag}</strong>
            <span className="govuk-phase-banner__text">{props.description}</span>
        </p>
    </div>;
}