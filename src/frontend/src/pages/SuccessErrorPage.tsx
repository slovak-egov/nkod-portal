import { ReactNode } from 'react';
import Alert from '../components/Alert';
import Button from '../components/Button';

type Props = {
    isSuccess: boolean;
    msg: ReactNode;
    backButtonLabel: string;
    backButtonClick: () => void;
};

export default function SuccessErrorPage(props: Props) {
    const { msg, backButtonLabel, backButtonClick, isSuccess } = props;
    return (
        <>
            <Alert type={isSuccess ? 'info' : 'warning'}>
                <div className="govuk-!-padding-4">
                    <span className="govuk-heading-m govuk-!-font-weight-bold">{msg}</span>
                    <div>
                        <Button buttonType="secondary" onClick={backButtonClick}>
                            {backButtonLabel}
                        </Button>
                    </div>
                </div>
            </Alert>
        </>
    );
}

SuccessErrorPage.defaultProps = {
    isSuccess: true
};
